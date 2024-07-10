using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Storage.Interface
{
    public interface IPageItem
    {
        public uint DataLength { get; set; }
        public IReadOnlyList<byte> Data { get; set; }
    }
}
