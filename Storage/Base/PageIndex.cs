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
            // this.PageOffset = 0; // Already initialized by default
        }

        public PageIndex(uint pageId, uint pageOffset) // Added constructor
        {
            this.PageId = pageId;
            this.PageOffset = pageOffset;
        }

        public PageIndex()
            : this(0, 0) // Updated to call the new constructor
        {}
    }
}
