using System;
using System.Collections.Generic;
using System.IO;
using VectorDB.Storage.Base;
using VectorDB.Storage.Interface;

namespace VectorDB.Storage
{
    public class PageManager : IDisposable
    {
        private readonly FileStream _dataFileStream; // For when persistence is added
        // private readonly FileStream _indexFileStream; // If a separate index file is used
        // private readonly Options _options; // Removed
        private readonly Dictionary<uint, IDataPage> _pageCache;
        private uint _nextPageId;
        private readonly uint _pageSize; // Consistent page size for all pages managed by this manager

        public PageManager(FileStream dataFileStream /* Options options */) // options parameter removed
        {
            _dataFileStream = dataFileStream; // Will be used more in persistence step
            // _options = options ?? throw new ArgumentNullException(nameof(options)); // Removed
            _pageCache = new Dictionary<uint, IDataPage>();
            // _nextPageId will be set based on file size.
            
            // Determine page size. For now, let's use BucketSize * VectorByteSize as a rough estimate for data pages.
            // This needs to be standardized, possibly from Options or a constant.
            // A fixed page size is generally easier to manage. Let's assume a default or calculate from options.
            // For simplicity, let's define a default page size if not directly in options.
            // For instance, 4096 bytes is a common page size.
            _pageSize = 4096; // Placeholder: This should be configurable or derived from options more robustly.
                               // e.g., _options.PageSize if we add it to Options class.
                               // Or, it could be related to _options.BucketSize and _options.VectorByteSize,
                               // but BucketSize seems more related to logical grouping than physical page size.

            // Initialize _nextPageId based on dataFileStream length
            if (_dataFileStream.Length > 0)
            {
                if (_dataFileStream.Length % _pageSize != 0)
                {
                    // This indicates a corrupted file or inconsistent page size.
                    // Handle this error appropriately, e.g., by throwing an exception or attempting recovery.
                    // For now, we'll throw an exception as it's a critical issue.
                    throw new InvalidDataException($"Data file length {_dataFileStream.Length} is not a multiple of page size {_pageSize}.");
                }
                _nextPageId = (uint)(_dataFileStream.Length / _pageSize);
                Console.WriteLine($"PageManager initialized. Existing file detected. NextPageId set to {_nextPageId} based on file length {_dataFileStream.Length} and page size {_pageSize}.");
            }
            else
            {
                _nextPageId = 0; // New database, start with page ID 0
                Console.WriteLine($"PageManager initialized. New or empty file. NextPageId set to 0.");
            }
        }

        /// <summary>
        /// Allocates a new data page.
        /// </summary>
        /// <param name="pageType">The type of page to allocate.</param>
        /// <param name="itemSize">Required for FixedItemSize pages, specifies the size of each item.</param>
        /// <returns>The allocated page.</returns>
        public IDataPage AllocateDataPage(uint itemSize)
        {
            uint newPageId = _nextPageId++;
            // For now, all data pages are of type DataPage.
            // PageType could be passed in if different types of data pages are needed.
            var pageType = PageType.Data | PageType.FixedItemSize | PageType.Primary;
            var dataPage = new DataPage(newPageId, _pageSize, itemSize, pageType);
            
            _pageCache.Add(newPageId, dataPage);
            Console.WriteLine($"Allocated new DataPage. PageId: {newPageId}, PageSize: {_pageSize}, ItemSize: {itemSize}");
            return dataPage;
        }

        /// <summary>
        /// Allocates a new header page (typically only one or few per DB).
        /// </summary>
        /// <returns>The allocated header page.</returns>
        public IDataPage AllocateHeaderPage()
        {
            // Header page ID is typically fixed, e.g., 0
            uint headerPageId = 0; 
            if (_pageCache.ContainsKey(headerPageId) || _nextPageId > 0) // A bit simplistic check for existing header
            {
                 // Logic to fetch/load existing header page if needed
                 // For now, if we call this, we assume we need a new one and it's page 0.
                 // Or, ensure it's only called once for a new DB.
                 if(_pageCache.ContainsKey(headerPageId)) return _pageCache[headerPageId];
            }
            
            var pageType = PageType.Header | PageType.Primary;
            // MaxItemSize is 0 for HeaderPage as per its constructor. PageSize is standard.
            // Assuming HeaderPage class exists and has a constructor like:
            // public HeaderPage(uint pageId, PageType type, uint pageSize, IPageIndex previousPage, IPageIndex nextPage, byte[] data)
            // For now, the last three params are null or default for a new HeaderPage.
            var headerPage = new HeaderPage(headerPageId, pageType, _pageSize, null, null, null);
            
            if (_nextPageId == 0) _nextPageId++; // Ensure next page ID is incremented past header if it's ID 0.

            _pageCache.Add(headerPageId, headerPage);
            Console.WriteLine($"Allocated new HeaderPage. PageId: {headerPageId}, PageSize: {_pageSize}");
            return headerPage;
        }


        /// <summary>
        /// Gets a page from the cache. In the future, this will also handle loading from disk.
        /// </summary>
        /// <param name="pageId">The ID of the page to retrieve.</param>
        /// <returns>The page if found, otherwise null.</returns>
        public IDataPage? GetPage(uint pageId)
        {
            if (_pageCache.TryGetValue(pageId, out var pageFromCache))
            {
                Console.WriteLine($"Page {pageId} found in cache.");
                return pageFromCache;
            }

            Console.WriteLine($"Page {pageId} not in cache. Attempting to load from disk...");
            long expectedPosition = (long)pageId * _pageSize;

            if (_dataFileStream == null || !_dataFileStream.CanRead || _dataFileStream.Length <= expectedPosition)
            {
                Console.WriteLine($"Page {pageId} cannot be loaded: data stream not available, not readable, or pageId is out of file bounds (File length: {_dataFileStream?.Length ?? -1}, Expected position: {expectedPosition}).");
                return null; // Page does not exist on disk or stream is invalid
            }

            IDataPage loadedPage;
            try
            {
                _dataFileStream.Seek(expectedPosition, SeekOrigin.Begin);
                // Using BinaryReader but leaving stream open with 'true'
                using (var reader = new BinaryReader(_dataFileStream, System.Text.Encoding.UTF8, true)) 
                {
                    // Peek at the PageType first to decide which Load method to call
                    PageType pageTypeFromFile = (PageType)reader.ReadInt32();
                    
                    // Rewind or re-seek so the Load method can read PageType again from start of page.
                    _dataFileStream.Seek(expectedPosition, SeekOrigin.Begin); 

                    if (((pageTypeFromFile & PageType.Header) != 0))
                    {
                        Console.WriteLine($"Loading HeaderPage {pageId}. Type read: {pageTypeFromFile}");
                        loadedPage = HeaderPage.Load(reader, pageId, _pageSize);
                    }
                    else if (((pageTypeFromFile & PageType.Data) != 0))
                    {
                        Console.WriteLine($"Loading DataPage {pageId}. Type read: {pageTypeFromFile}");
                        // DataPage.Load will read MaxItemSize itself from the page metadata.
                        loadedPage = DataPage.Load(reader, pageId, _pageSize);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown page type {pageTypeFromFile} read from file for PageId {pageId}.");
                        throw new InvalidDataException($"Unknown page type {pageTypeFromFile} read from file for PageId {pageId}.");
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                Console.WriteLine($"Error loading page {pageId}: End of stream reached prematurely. {ex.Message}");
                // This might happen if the file is truncated or page data is incomplete.
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading page {pageId}: {ex.Message}");
                // Other exceptions during load (e.g., InvalidDataException from Load methods)
                throw; // Re-throw for higher level handling if needed
            }

            if (loadedPage != null)
            {
                _pageCache.Add(pageId, loadedPage);
                Console.WriteLine($"Page {pageId} loaded from disk and cached.");
                return loadedPage;
            }
            return null; // Should not be reached if exceptions are handled/thrown
        }

        /// <summary>
        /// Saves a page to disk.
        /// </summary>
        /// <param name="page">The page to save.</param>
        public void SavePage(IDataPage page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            if (_dataFileStream == null || !_dataFileStream.CanWrite)
            {
                Console.WriteLine($"Data file stream is not available or not writable. Cannot save page {page.PageId}.");
                throw new InvalidOperationException("Data file stream is not available or not writable.");
            }

            long position = (long)page.PageId * _pageSize;
            _dataFileStream.Seek(position, SeekOrigin.Begin);

            // Using BinaryWriter but leaving stream open with 'true'
            using (var writer = new BinaryWriter(_dataFileStream, System.Text.Encoding.UTF8, true))
            {
                page.Save(writer);
            }
            _dataFileStream.Flush(); // Ensure data is written to the OS.

            page.PageStatus &= ~PageStatus.Dirty; // Mark page as clean after saving
            Console.WriteLine($"Page {page.PageId} saved to disk at position {position}.");
        }
        
        /// <summary>
        /// Flushes all dirty pages in the cache to disk. (Placeholder for now)
        /// </summary>
        public void FlushAllDirtyPages()
        {
            Console.WriteLine("Placeholder: FlushAllDirtyPages called. Iterating through cache...");
            foreach (var pageEntry in _pageCache)
            {
                if ((pageEntry.Value.PageStatus & PageStatus.Dirty) != 0)
                {
                    SavePage(pageEntry.Value);
                }
            }
        }

        public uint GetNextPageId() // Added
        {
            return _nextPageId;
        }

        public void Dispose()
        {
            FlushAllDirtyPages(); // Ensure data is saved on dispose
            // _dataFileStream?.Dispose(); // The owner of the stream should dispose it (VectorDBEngine)
            // _indexFileStream?.Dispose();
            _pageCache.Clear();
        }
    }
}
