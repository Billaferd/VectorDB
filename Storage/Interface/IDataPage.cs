using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Interface
{
    public interface IDataPage<T>
    {
        public uint PageId { get; init; }
        
        public uint PageSize { get; init; }

        public uint MaxItems { get; init; }

        public List<uint> ItemOffsets { get; init; }

        public List<T> PageItems { get; init; }
    }
}
