using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorDB.Storage.Interface;

using System.IO; // Added for BinaryWriter/Reader

namespace VectorDB.Storage.Base
{
    public class HeaderPage // Changed from struct to class
        : IDataPage
    {
        private uint _pageSize; // Added
        public const uint PersistedSize = 52; // Added

        public PageType PageType { get; set; }
        public PageStatus PageStatus { get; set; } = PageStatus.Ok;
        public uint PageId { get; set; } // Changed from init to set
        public uint FreeSpace { get; set; }
        public uint MaxItemSize { get; set; } // Changed from init to set
        public IPageIndex PreviousPage { get; set; }
        public IPageIndex NextPage { get; set; }
        public IList<uint> DataOffsets { get; set; }

        // Private constructor for Load method
        private HeaderPage() {
            // Initialize lists and PageIndex to avoid null references if they are not set by Load
            // This depends on Load method's completeness.
            // For now, assuming Load will set these. If Load might skip some, initialize here.
            PreviousPage = new PageIndex();
            NextPage = new PageIndex();
            DataOffsets = new List<uint>();
        }

        public HeaderPage(uint id, PageType type, uint pageSize, IPageIndex? prevPage, IPageIndex? nextPage, IList<uint>? offsets)
        {
            this.PageId = id;
            this.PageType = type;
            this._pageSize = pageSize; // Added
            // FreeSpace calculation should consider only the space available for *data items*,
            // which for HeaderPage is typically none beyond its fixed persisted structure.
            // The PersistedSize (52 bytes) is the size of the header's own metadata on disk.
            // FreeSpace in the context of a HeaderPage might mean space *within* its PersistedSize structure
            // if it could store variable data, or it might refer to the rest of the page if _pageSize > PersistedSize.
            // Given HeaderPage usually doesn't store additional data like DataPage,
            // FreeSpace is calculated relative to _pageSize, but its utility is low if no items are added.
            // Let's assume FreeSpace means space available on this page beyond its own header structure.
            // If _pageSize == PersistedSize, then FreeSpace would be 0.
            // The original calculation `pageSize - 52` implied that 52 bytes are used by the header structure.
            this.FreeSpace = pageSize - PersistedSize; 
            this.MaxItemSize = 0; // Header page does not store items with variable sizes in the same way DataPage does.
            this.PreviousPage = prevPage ?? new PageIndex();
            this.NextPage = nextPage ?? new PageIndex();
            // Ensure DataOffsets is initialized. If offsets are passed, use them, otherwise empty list.
            this.DataOffsets = offsets ?? new List<uint>();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((int)PageType);
            writer.Write((int)PageStatus);
            writer.Write(PageId);
            writer.Write(FreeSpace); // This should be updated if any data was "added" to header page.
            writer.Write(MaxItemSize);
            writer.Write(PreviousPage.PageId);
            writer.Write(PreviousPage.PageOffset);
            writer.Write(NextPage.PageId);
            writer.Write(NextPage.PageOffset);
            
            writer.Write(DataOffsets.Count);
            int offsetsToWrite = Math.Min(DataOffsets.Count, (int)((PersistedSize - (writer.BaseStream.Position % PersistedSize)) / sizeof(uint))); // Ensure we don't write more than PersistedSize allows
            
            for (int i = 0; i < offsetsToWrite; i++)
            {
                writer.Write(DataOffsets[i]);
            }

            // Pad with zeros if necessary to reach PersistedSize
            // Current position: 4+4+4+4+4+8+8+4 = 40 bytes + (offsetsToWrite * 4)
            long currentPosition = 40 + (long)offsetsToWrite * sizeof(uint); // This is the position *within* the PersistedSize block
            long metadataPaddingNeeded = PersistedSize - currentPosition;
            if (metadataPaddingNeeded < 0) throw new InvalidOperationException($"HeaderPage persisted data ({currentPosition} bytes) exceeds PersistedSize ({PersistedSize} bytes).");

            for (int i = 0; i < metadataPaddingNeeded; i++)
            {
                writer.Write((byte)0);
            }

            // After writing PersistedSize (52 bytes), pad the rest of the page to reach _pageSize
            long pagePaddingNeeded = _pageSize - PersistedSize;
            if (pagePaddingNeeded < 0) // Should not happen if _pageSize >= PersistedSize (checked in constructor)
            {
                throw new InvalidOperationException($"PageSize ({_pageSize}) is less than PersistedSize ({PersistedSize}). Cannot pad header page correctly.");
            }
            for (long i = 0; i < pagePaddingNeeded; i++)
            {
                writer.Write((byte)0);
            }
        }

        public static HeaderPage Load(BinaryReader reader, uint expectedPageId, uint pageSizeFromManager)
        {
            HeaderPage instance = new HeaderPage();
            instance._pageSize = pageSizeFromManager;

            instance.PageType = (PageType)reader.ReadInt32();
            instance.PageStatus = (PageStatus)reader.ReadInt32();
            instance.PageId = reader.ReadUInt32();
            instance.FreeSpace = reader.ReadUInt32();
            instance.MaxItemSize = reader.ReadUInt32();
            
            uint prevPageId = reader.ReadUInt32();
            uint prevPageOffset = reader.ReadUInt32();
            instance.PreviousPage = new PageIndex(prevPageId, prevPageOffset);

            uint nextPageId = reader.ReadUInt32();
            uint nextPageOffset = reader.ReadUInt32();
            instance.NextPage = new PageIndex(nextPageId, nextPageOffset);

            int dataOffsetsCount = reader.ReadInt32();
            instance.DataOffsets = new List<uint>(dataOffsetsCount);
            for (int i = 0; i < dataOffsetsCount; i++)
            {
                instance.DataOffsets.Add(reader.ReadUInt32());
            }

            if (instance.PageId != expectedPageId)
            {
                throw new InvalidDataException($"Loaded page ID {instance.PageId} does not match expected ID {expectedPageId}.");
            }

            // Consume any padding to align to PersistedSize
            long currentPosition = 40 + (long)dataOffsetsCount * sizeof(uint);
            long paddingToConsume = PersistedSize - currentPosition;
             if (paddingToConsume < 0) throw new InvalidOperationException($"HeaderPage loaded data implies size ({currentPosition} bytes) inconsistent with PersistedSize ({PersistedSize} bytes) before padding.");

            if (paddingToConsume > 0)
            {
                reader.ReadBytes((int)paddingToConsume);
            }
            
            return instance;
        }

        // HeaderPage does not support adding items in the same way DataPage does.
        public bool TryAddItem(byte[] itemData)
        {
            // Console.WriteLine("Warning: TryAddItem called on HeaderPage. Not supported for general data.");
            // Or throw new NotSupportedException("HeaderPage does not support adding items directly.");
            return false; 
        }

        public byte[]? GetItem(int itemIndex)
        {
            // Console.WriteLine("Warning: GetItem called on HeaderPage. Not supported for general data.");
            // Or throw new NotSupportedException("HeaderPage does not support retrieving items directly.");
            return null;
        }
    }
}
