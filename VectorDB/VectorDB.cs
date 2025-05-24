using System;
using System.IO;
using VectorDB.Storage;
using VectorDB.Storage.Interface;
using VectorDB.Vector.Interface;
using VectorDB.Vectors; // Added for DistanceMetric, VectorMath, ImmutableFloatVector
using System.Text;
using System.Collections.Generic; // Added for List
using System.Linq; // Added for OrderByDescending, Take

namespace VectorDB
{
    // Moved SimilaritySearchResult here as requested by the prompt structure,
    // or it could be in its own file.
    public class SimilaritySearchResult
    {
        // Using string for Id for now, assuming we'll map internal page/slot to external ID later
        public string Id { get; set; }
        public float Score { get; set; }
        // public IVector<float> Vector { get; set; } // Optionally return the vector itself

        public SimilaritySearchResult(string id, float score)
        {
            Id = id;
            Score = score;
        }
    }

    public class VectorDBEngine : IDisposable
    {
        private readonly Options _options;
        private FileStream _dataFileStream;
        private FileStream? _indexFileStream; // Made nullable
        private PageManager _pageManager; // Modified
        private uint? _lastDataPageId = null; // Added

        public VectorDBEngine(Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // Validate options
            if (string.IsNullOrWhiteSpace(_options.DataFilePath))
            {
                throw new ArgumentException("DataFilePath cannot be empty.", nameof(options));
            }
            // IndexFilePath can be optional if index is combined or not used initially
            // if (string.IsNullOrWhiteSpace(_options.IndexFilePath))
            // {
            //     throw new ArgumentException("IndexFilePath cannot be empty.", nameof(options));
            // }
            if (_options.VectorBitSize <= 0 || _options.VectorBitSize % 8 != 0)
            {
                throw new ArgumentException("VectorBitSize must be a positive multiple of 8.", nameof(options));
            }
            _options.VectorByteSize = _options.VectorBitSize / 8; // Ensure this is correctly set based on VectorBitSize

            // Further initialization will be done in an Open() or Create() method
        }

        public void CreateOrOpen()
        {
            bool dataFileExists = File.Exists(_options.DataFilePath);
            // bool indexFileExists = !string.IsNullOrWhiteSpace(_options.IndexFilePath) && File.Exists(_options.IndexFilePath);

            _dataFileStream = new FileStream(_options.DataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            
            // if (!string.IsNullOrWhiteSpace(_options.IndexFilePath))
            // {
            //     _indexFileStream = new FileStream(_options.IndexFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            // }

            // Removed the initial block that called InitializeNewDatabase/LoadDatabaseHeader
            // before _pageManager was initialized.

            // PageManager must be initialized after _dataFileStream is ready.
            _pageManager = new PageManager(_dataFileStream /*, _options */); // _options removed

            // Now, based on file state (which PageManager also checked for _nextPageId),
            // initialize or load database metadata.
            if (_dataFileStream.Length == 0) // Check actual length after PageManager might have been created
            {
                // Treat as new, even if file existed but was empty
                Console.WriteLine($"Data file '{_options.DataFilePath}' is empty. Initializing new database.");
                InitializeNewDatabase();
            }
            else
            {
                // File has content, load existing metadata
                Console.WriteLine($"Opened existing data file: '{_options.DataFilePath}' with length {_dataFileStream.Length}. Loading header.");
                LoadDatabaseHeader();
            }
        }

        private void InitializeNewDatabase()
        {
            Console.WriteLine("Initializing new database structure...");
            if (_pageManager == null) throw new InvalidOperationException("PageManager not initialized for InitializeNewDatabase.");
            var headerPage = _pageManager.AllocateHeaderPage();
            _pageManager.SavePage(headerPage); // Save the newly allocated header page
            Console.WriteLine($"Allocated and saved header page {headerPage.PageId}.");
        }

        private void LoadDatabaseHeader()
        {
            Console.WriteLine("Loading database header...");
            if (_pageManager == null) throw new InvalidOperationException("PageManager not initialized for LoadDatabaseHeader.");
            
            var headerPage = _pageManager.GetPage(0); // GetPage will load from disk if not cached
            
            if (headerPage == null)
            {
                // This case should ideally be handled by PageManager.GetPage if page 0 truly doesn't exist,
                // or it might indicate corruption if we expected page 0.
                // If GetPage returns null, it means the page couldn't be loaded from disk (e.g. file too short for page 0)
                Console.WriteLine($"Critical error: Header page (Page 0) could not be loaded from file {_options.DataFilePath}. The file might be corrupted or too short.");
                throw new InvalidDataException($"Header page (Page 0) could not be loaded from file {_options.DataFilePath}.");
            }

            if ((headerPage.PageType & PageType.Header) == 0)
            {
                Console.WriteLine($"Critical error: Page 0 loaded from {_options.DataFilePath} is not a Header page. Actual type: {headerPage.PageType}.");
                throw new InvalidDataException($"Page 0 loaded from {_options.DataFilePath} is not a Header page. Actual type: {headerPage.PageType}.");
            }
            
            Console.WriteLine($"Successfully loaded header page {headerPage.PageId}. Type: {headerPage.PageType}.");
            // _nextPageId logic is now handled by PageManager constructor based on file size.
            // Any other header data (e.g., specific database settings) would be read from headerPage here.
        }

        public void AddVector(IVector<float> vector, string externalId)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (vector.VectorByteLength != _options.VectorByteSize)
            {
                throw new ArgumentException($"Vector byte length {vector.VectorByteLength} does not match database option VectorByteSize {_options.VectorByteSize}.", nameof(vector));
            }

            // For now, externalId is not stored directly with the vector in this simplified approach.
            // It will be handled by a separate indexing mechanism later.
            // We will serialize only the vector data.
            
            // Convert float vector to byte array
            byte[] itemData = new byte[vector.VectorByteLength];
            // Assuming vector.Vector is IReadOnlyList<float> and can be converted to float[] for Buffer.BlockCopy
            // If vector.Vector is already float[], direct use is fine.
            // If it's List<float>, .ToArray() is good. For IReadOnlyList<float>, we might need to copy element by element if it's not already an array.
            // For simplicity, assuming ToArray() is available or vector.Vector is directly usable in a way Buffer.BlockCopy expects.
            // A common pattern is ((List<float>)vector.Vector).CopyTo(floatArray) or similar.
            // Let's stick to the provided ToArray() for now, assuming an appropriate implementation for IVector<float>.
            // For ImmutableFloatVector, vector.Vector.ToArray() would work if Vector is List or array.
            // If IReadOnlyList<float> is guaranteed to be backed by an array or List<T>, .ToArray() (LINQ) is fine.
            // More robust: Create a float array and copy into it.
            float[] floatArray = new float[vector.Vector.Count]; // Assuming IReadOnlyList has Count
            for(int i=0; i < vector.Vector.Count; i++) floatArray[i] = vector.Vector[i];
            Buffer.BlockCopy(floatArray, 0, itemData, 0, itemData.Length);


            uint itemSize = (uint)itemData.Length;
            IDataPage dataPage = null;

            if (_lastDataPageId.HasValue)
            {
                dataPage = _pageManager.GetPage(_lastDataPageId.Value);
                if (dataPage != null && dataPage.PageType == PageType.Data && dataPage.FreeSpace < itemSize) // Basic check
                {
                    dataPage = null; // Mark as needing new page
                }
            }

            if (dataPage == null)
            {
                dataPage = _pageManager.AllocateDataPage(itemSize);
                _lastDataPageId = dataPage.PageId;
            }

            if (!dataPage.TryAddItem(itemData))
            {
                // The page was full even after allocation, or item too large for initial free space after header
                // (This might happen if itemSize > (pageSize - EstimatedHeaderSize))
                // Or, the page we retrieved was full. Allocate a new one.
                dataPage = _pageManager.AllocateDataPage(itemSize);
                _lastDataPageId = dataPage.PageId;

                if (!dataPage.TryAddItem(itemData))
                {
                    // If it still fails, there's a more fundamental issue (e.g., item larger than any page can hold)
                    throw new InvalidOperationException($"Failed to add vector to page {dataPage.PageId}. Item size: {itemSize}, Page free space: {dataPage.FreeSpace}. This may occur if the vector size exceeds the usable space in a new page.");
                }
            }
            
            
            Console.WriteLine($"Added vector (ID: '{externalId}') to page {dataPage.PageId}.");
            _pageManager.SavePage(dataPage); // Save the data page after adding the item
        }

        public IVector<float>? GetVector(string externalId)
        {
            // This is a placeholder. Actual implementation requires an index
            // to map externalId to a page and offset.
            Console.WriteLine($"GetVector for ID '{externalId}' called. Indexing not yet implemented.");
            return null; 
            // throw new NotImplementedException("Vector retrieval by external ID requires an indexing mechanism not yet implemented.");
        }

        public void Dispose()
        {
            _pageManager?.Dispose(); // Dispose page manager first
            _dataFileStream?.Dispose();
            _indexFileStream?.Dispose(); // If used
            Console.WriteLine("VectorDBEngine disposed.");
        }

        public List<SimilaritySearchResult> FindSimilarVectors(IVector<float> queryVector, int k, DistanceMetric metric)
        {
            if (queryVector == null) throw new ArgumentNullException(nameof(queryVector));
            if (queryVector.VectorByteLength != _options.VectorByteSize)
            {
                throw new ArgumentException($"Query vector byte length {queryVector.VectorByteLength} does not match database option VectorByteSize {_options.VectorByteSize}.", nameof(queryVector));
            }
            if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "Number of results (k) must be positive.");

            var results = new List<SimilaritySearchResult>();

            // Iterate through all pages that could contain data.
            // _pageManager._nextPageId gives an upper bound on page IDs.
            // We need a way to know which pages are data pages.
            // For now, assume all pages from 1 upwards (0 is header) could be data pages.
            // This needs refinement: PageManager should provide a way to iterate data pages.
            
            uint maxPageId = _pageManager.GetNextPageId(); // Add GetNextPageId() to PageManager

            for (uint pageId = 1; pageId < maxPageId; pageId++) // Page 0 is header
            {
                IDataPage page = _pageManager.GetPage(pageId);
                if (page != null && (page.PageType & PageType.Data) != 0)
                {
                    for (int i = 0; i < page.DataOffsets.Count; i++)
                    {
                        byte[] itemData = page.GetItem(i);
                        if (itemData == null || itemData.Length != _options.VectorByteSize)
                        {
                            // Skip malformed or incorrect size data
                            Console.WriteLine($"Skipping item {i} on page {pageId} due to null or incorrect size.");
                            continue;
                        }

                        // Deserialize byte[] back to float[]
                        // This assumes the itemData is purely the float vector.
                        float[] vectorValues = new float[_options.VectorByteSize / sizeof(float)];
                        Buffer.BlockCopy(itemData, 0, vectorValues, 0, itemData.Length);
                        
                        // Create an IVector<float> from these values.
                        // We need a concrete implementation of IVector to pass to VectorMath.
                        // Assuming an existing ImmutableFloatVector or similar:
                        var storedVector = new VectorDB.Vectors.ImmutableFloatVector(vectorValues); // Using the one from VectorDB.Vectors namespace

                        float score = 0;
                        switch (metric)
                        {
                            case DistanceMetric.EuclideanDistance:
                                score = VectorMath.EuclideanDistance(queryVector, storedVector);
                                break;
                            case DistanceMetric.CosineSimilarity:
                                score = VectorMath.CosineSimilarity(queryVector, storedVector);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(metric), "Unsupported distance metric.");
                        }

                        // For Euclidean distance, lower is better. For Cosine similarity, higher is better.
                        // We want to keep the 'k' best scores.
                        // This simple version adds all and then sorts. More efficient would be a bounded priority queue.
                        // The ID is problematic here, as we only stored raw vectors.
                        // For now, use "pageId_itemId" as a temporary ID.
                        results.Add(new SimilaritySearchResult($"page{pageId}_item{i}", score));
                    }
                }
            }

            // Sort results: Euclidean (ascending), Cosine (descending)
            if (metric == DistanceMetric.EuclideanDistance)
            {
                return results.OrderBy(r => r.Score).Take(k).ToList();
            }
            else // CosineSimilarity
            {
                return results.OrderByDescending(r => r.Score).Take(k).ToList();
            }
        }
    }
}
