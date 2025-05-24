using System;
using System.Collections.Generic;
using System.Linq;
using VectorDB.Storage.Interface;
using System.IO; // Added for BinaryWriter/Reader

namespace VectorDB.Storage.Base
{
    public class DataPage : IDataPage
    {
        public PageType PageType { get; set; }
        public PageStatus PageStatus { get; set; }
        public uint PageId { get; set; } // Changed from init to set
        public uint FreeSpace { get; set; }
        public uint MaxItemSize { get; set; } // Changed from init to set
        public IPageIndex PreviousPage { get; set; }
        public IPageIndex NextPage { get; set; }
        public IList<uint> DataOffsets { get; set; } // Stores offsets of items within this page's data buffer.

        private byte[] _pageData; // The actual byte buffer for the page
        private uint _pageSize;   // Total size of the page buffer
        private uint _currentDataEndOffset; // Tracks the end of data within the page buffer

        // Constants for typical page header/metadata size. This should be configurable or standardized.
        // For now, let's assume a placeholder value. This needs to be accurate for FreeSpace calculation.
        // This should account for PageType, PageStatus, PageId, FreeSpace, MaxItemSize,
        // PreviousPage Ptr, NextPage Ptr, DataOffsets list size/count.
        // Let's estimate a header size for now.
        // For example: PageType (4), PageStatus (4), PageId (4), FreeSpace (4), MaxItemSize (4) = 20 bytes
        // PrevPageId (4), PrevPageOffset (4) = 8 bytes
        // NextPageId (4), NextPageOffset (4) = 8 bytes
        // DataOffsets count (4) + capacity for offsets (e.g. 10 offsets * 4 bytes/offset = 40) = 44 bytes
        // Total estimated header = 20 + 8 + 8 + 44 = 80 bytes.
        // This is a rough estimate and will need refinement.
        public const uint PersistedMetadataSize = 80; // Renamed from EstimatedHeaderSize

        // Private constructor for Load method
        private DataPage() {
            // Initialize to avoid nulls, Load will overwrite these.
            PreviousPage = new PageIndex();
            NextPage = new PageIndex();
            DataOffsets = new List<uint>();
            _pageData = Array.Empty<byte>(); // Will be sized by Load
        }

        public DataPage(uint pageId, uint pageSize, uint itemSize, PageType type = PageType.Data | PageType.FixedItemSize | PageType.Primary)
        {
            if (pageSize <= PersistedMetadataSize) // Changed
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than persisted metadata size.");
            if ((type & PageType.FixedItemSize) == 0)
                throw new ArgumentException("Currently, DataPage only supports FixedItemSize.", nameof(type));
            if (itemSize == 0 && (type & PageType.FixedItemSize) != 0)
                throw new ArgumentException("ItemSize must be greater than 0 for FixedItemSize pages.", nameof(itemSize));

            PageId = pageId;
            _pageSize = pageSize;
            PageType = type;
            PageStatus = PageStatus.Ok;
            MaxItemSize = itemSize; // For fixed size, this is the size of each item.
            
            _pageData = new byte[_pageSize];
            // Copy PersistedMetadataSize to the start of _pageData as 0s, or ensure Save/Load handle metadata separately
            // For now, _pageData is the *entire* page including where metadata *would* go if stored inline.
            // The actual items start after PersistedMetadataSize offset within _pageData.
            
            FreeSpace = _pageSize - PersistedMetadataSize; // Initial free space calculation
            _currentDataEndOffset = PersistedMetadataSize; // Data items start after the metadata area

            PreviousPage = new PageIndex(0); // Unlinked by default
            NextPage = new PageIndex(0);     // Unlinked by default
            DataOffsets = new List<uint>();
        }

        /// <summary>
        /// Tries to add an item (byte array) to the page.
        /// For FixedItemSize, the item.Length must match MaxItemSize.
        /// </summary>
        /// <param name="itemData">The byte array representing the item.</param>
        /// <returns>True if item was added, false otherwise (e.g., not enough space).</returns>
        public bool TryAddItem(byte[] itemData)
        {
            if ((PageType & PageType.FixedItemSize) != 0)
            {
                if (itemData.Length != MaxItemSize)
                {
                    // Or throw new ArgumentException("Item size does not match MaxItemSize for FixedItemSize page.");
                    return false; 
                }
            }

            // Check if there's enough space for the item itself and its offset.
            // Each offset takes approx 4 bytes in the DataOffsets list.
            // This space calculation for DataOffsets list growth is simplified here.
            uint requiredSpace = (uint)itemData.Length;
            if (DataOffsets.Count > 0) // Simplified check, assumes offsets storage is part of FreeSpace
                 requiredSpace += sizeof(uint); 


            if (FreeSpace < requiredSpace)
            {
                return false; // Not enough space
            }

            // Check if data can fit into the _pageData buffer based on _currentDataEndOffset
            if (_currentDataEndOffset + itemData.Length > _pageSize) {
                 // This implies an issue with FreeSpace calculation or internal fragmentation not handled by this simple model.
                 // For now, assume FreeSpace is the primary guard.
                return false;
            }

            Buffer.BlockCopy(itemData, 0, _pageData, (int)_currentDataEndOffset, itemData.Length);
            DataOffsets.Add(_currentDataEndOffset);
            
            FreeSpace -= requiredSpace;
            _currentDataEndOffset += (uint)itemData.Length;
            PageStatus |= PageStatus.Dirty;

            return true;
        }

        /// <summary>
        /// Retrieves an item from the page using its index in the DataOffsets list.
        /// </summary>
        /// <param name="itemIndex">The index of the item (corresponds to DataOffsets list index).</param>
        /// <returns>Byte array of the item, or null if index is invalid.</returns>
        public byte[]? GetItem(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= DataOffsets.Count)
            {
                return null; // Or throw new IndexOutOfRangeException();
            }

            uint itemOffset = DataOffsets[itemIndex];
            uint itemSize = MaxItemSize; // Assuming FixedItemSize

            // For FluidItemSize, you would need to determine the size,
            // e.g., by looking at the next item's offset or storing sizes.
            // if ((PageType & PageType.FluidItemSize) != 0) {
            //     itemSize = (itemIndex == DataOffsets.Count - 1) ? 
            //                _currentDataEndOffset - itemOffset : 
            //                DataOffsets[itemIndex + 1] - itemOffset;
            // }
            
            if (itemOffset + itemSize > _currentDataEndOffset) // Make sure we don't read past written data
            {
                 // Should not happen if DataOffsets and _currentDataEndOffset are managed correctly
                return null; 
            }

            byte[] itemData = new byte[itemSize];
            Buffer.BlockCopy(_pageData, (int)itemOffset, itemData, 0, (int)itemSize);
            return itemData;
        }
        
        // GetItem (by offset) could also be useful
        // RemoveItem would require more complex logic (e.g., compacting data or marking as deleted)

        public void Save(BinaryWriter writer)
        {
            // Corrected Save logic:
            // 1. Serialize metadata into the beginning of the _pageData buffer.
            // 2. Write the entire _pageData buffer (now containing metadata + item data) to the main stream.

            using (var ms = new MemoryStream(_pageData, 0, (int)PersistedMetadataSize, true))
            using (var metadataWriter = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
            {
                metadataWriter.Write((int)PageType);
                metadataWriter.Write((int)PageStatus);
                metadataWriter.Write(PageId);
                metadataWriter.Write(FreeSpace);
                metadataWriter.Write(MaxItemSize);
                metadataWriter.Write(PreviousPage.PageId);
                metadataWriter.Write(PreviousPage.PageOffset);
                metadataWriter.Write(NextPage.PageId);
                metadataWriter.Write(NextPage.PageOffset);

                metadataWriter.Write(DataOffsets.Count);
                
                // Calculate how many offsets can actually be written into PersistedMetadataSize
                // Base size of fixed metadata: PageType to NextPage.Offset (9 uints = 36 bytes) + DataOffsets.Count (int = 4 bytes) = 40 bytes
                int fixedMetadataFieldsSize = (9 * sizeof(uint)) + sizeof(int); // (5 fields + 2xPageIndex) + Count
                int maxOffsetsStorable = (int)(PersistedMetadataSize - fixedMetadataFieldsSize) / sizeof(uint);
                int actualOffsetsToWrite = Math.Min(DataOffsets.Count, maxOffsetsStorable);

                for (int i = 0; i < actualOffsetsToWrite; i++)
                {
                    metadataWriter.Write(DataOffsets[i]);
                }

                // Pad the rest of the metadata area in _pageData with zeros
                long finalMetadataPosition = metadataWriter.BaseStream.Position;
                long currentMetadataPaddingNeeded = PersistedMetadataSize - finalMetadataPosition;
                if (currentMetadataPaddingNeeded < 0)
                {
                    throw new InvalidOperationException($"Internal error: Metadata ({finalMetadataPosition} bytes) written to _pageData exceeds PersistedMetadataSize ({PersistedMetadataSize} bytes). Offsets to write: {actualOffsetsToWrite}, DataOffsets.Count: {DataOffsets.Count}");
                }
                for (int i = 0; i < currentMetadataPaddingNeeded; i++)
                {
                    metadataWriter.Write((byte)0);
                }
            }

            // Now the _pageData byte array has its metadata section correctly populated.
            // Item data is already in _pageData from PersistedMetadataSize onwards (placed by TryAddItem).
            writer.Write(_pageData, 0, (int)_pageSize); // Write the entire prepared page buffer.
        }

        public static DataPage Load(BinaryReader reader, uint expectedPageId, uint pageSizeFromManager)
        {
            DataPage instance = new DataPage();
            instance._pageSize = pageSizeFromManager;
            instance._pageData = new byte[pageSizeFromManager];

            int bytesRead = reader.Read(instance._pageData, 0, (int)pageSizeFromManager);
            if (bytesRead != pageSizeFromManager)
            {
                throw new EndOfStreamException($"Expected to read {pageSizeFromManager} bytes for page data, but only got {bytesRead}.");
            }

            using (var ms = new MemoryStream(instance._pageData, 0, (int)PersistedMetadataSize, false))
            using (var metadataReader = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
            {
                instance.PageType = (PageType)metadataReader.ReadInt32();
                instance.PageStatus = (PageStatus)metadataReader.ReadInt32();
                instance.PageId = metadataReader.ReadUInt32();
                instance.FreeSpace = metadataReader.ReadUInt32();
                instance.MaxItemSize = metadataReader.ReadUInt32();
                instance.PreviousPage = new PageIndex(metadataReader.ReadUInt32(), metadataReader.ReadUInt32());
                instance.NextPage = new PageIndex(metadataReader.ReadUInt32(), metadataReader.ReadUInt32());
                
                int dataOffsetsCount = metadataReader.ReadInt32();
                instance.DataOffsets = new List<uint>(dataOffsetsCount);
                long baseMetaSize = 40; 
                int maxOffsetsStorableInLoad = (int)(PersistedMetadataSize - baseMetaSize) / sizeof(uint);
                int offsetsToRead = Math.Min(dataOffsetsCount, maxOffsetsStorableInLoad);

                for (int i = 0; i < offsetsToRead; i++)
                {
                    instance.DataOffsets.Add(metadataReader.ReadUInt32());
                }
            }

            if (instance.PageId != expectedPageId)
            {
                throw new InvalidDataException($"Loaded page ID {instance.PageId} does not match expected ID {expectedPageId}.");
            }
            
            instance._currentDataEndOffset = PersistedMetadataSize + (uint)instance.DataOffsets.Count * instance.MaxItemSize;

            return instance;
        }
    }
}
