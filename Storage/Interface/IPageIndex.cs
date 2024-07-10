using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Storage.Interface
{
    public interface IPageIndex
    {
        public uint PageId { get; set; }
        public uint PageOffset { get; set; }
    }
}
