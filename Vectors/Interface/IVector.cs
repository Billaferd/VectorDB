using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Vector.Interface
{
    public interface IVector<out T1>
    where T1 : struct
    {
        int VectorBitLength { get; }
        int VectorByteLength { get; }
        int SegmentBitLength { get; }
        int SegmentByteLength { get; }
        IReadOnlyList<T1> Vector { get; }
        string GetBucketHash(int segments);
    }
}
