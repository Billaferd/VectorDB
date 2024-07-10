using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Storage.Interface
{
    public interface IDataPage
    {
        public PageType PageType { get; set; }

        public PageStatus PageStatus { get; set; }

        public uint PageId { get; init; }

        public uint FreeSpace { get; set; }

        public uint MaxItemSize { get; init; }

        public IPageIndex PreviousPage { get; set; }

        public IPageIndex NextPage { get; set; }

        public IList<uint> DataOffsets { get; set; }
    }

    [Flags]
    public enum PageType
    {
        // Type
        Header = 0b0001_0000,
        Data = 0b0010_0000,
        Index = 0b0100_0000,
        WriteAhead = 0b1000_0000,

        // SubType
        Primary = 0b0000_0001,
        Overflow = 0b0000_0010,

        // Data Layout
        FixedItemSize = 0b0000_0100,
        FluidItemSize = 0b0000_1000,
    }

    [Flags]
    public enum PageStatus
    {
        Ok = 0b0000_0000,
        Deleted = 0b000_0001,
        Dirty = 0b0000_0010,
    }
}
