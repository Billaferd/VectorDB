using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorDB.Storage.Interface;

namespace VectorDB.Storage.Base
{
    public struct HeaderPage
        : IDataPage
    {
        public PageType PageType { get; set; }
        public PageStatus PageStatus { get; set; } = PageStatus.Ok;
        public uint PageId { get; init; }
        public uint FreeSpace { get; set; }
        public uint MaxItemSize { get; init; }
        public IPageIndex PreviousPage { get; set; }
        public IPageIndex NextPage { get; set; }
        public IList<uint> DataOffsets { get; set; }

        public HeaderPage(uint id, PageType type, uint pageSize, IPageIndex? prevPage, IPageIndex? nextPage, IList<uint>? offsets)
        {
            this.PageId = id;
            this.PageType = type;
            this.FreeSpace = pageSize - 52;
            this.MaxItemSize = 0;
            this.PreviousPage = prevPage ?? new PageIndex();
            this.NextPage = nextPage ?? new PageIndex();
            this.DataOffsets = new List<uint>();
        }
    }
}
