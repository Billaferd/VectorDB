using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorDB.Storage.Interface;

namespace VectorDB.Storage.Base
{
    public struct PageIndex
        : IPageIndex
    {
        public uint PageId { get; set; }
        public uint PageOffset { get; set; } = 0;

        public PageIndex(uint pageId)
        {
            this.PageId = pageId;
        }

        public PageIndex()
            : this(0)
        {}
    }
}
