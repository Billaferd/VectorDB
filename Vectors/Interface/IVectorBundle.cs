using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Vector.Interface
{
    public interface IVectorBundle<T1>
        where T1 : struct, IVector<T1>
    {
        public uint ItemCount { get; init; }
        public List<uint> ItemOffsets { get; init; }
        public List<IVector<T1>> Items { get; init; }
    }
}
