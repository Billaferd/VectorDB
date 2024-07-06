using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Vector.Interface
{
    public interface IVector<T1>
        where T1 : struct
    {
        public ImmutableArray<T1> Vector { get; init; }

        public int VectorBitLength { get; init; }

        public int VectorByteLength { get; init; }

        public string ToBucketString(int bucketSize);
    }
}
