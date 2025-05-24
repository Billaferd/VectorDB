using VectorDB.Vector.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VectorDB.Vectors // Changed namespace to match other files in Vectors project
{
    public class ImmutableFloatVector : IVector<float>
    {
        public IReadOnlyList<float> Vector { get; }
        public int VectorBitLength => Vector.Count * sizeof(float) * 8;
        public int VectorByteLength => Vector.Count * sizeof(float);
        public int SegmentBitLength => sizeof(float) * 8; // Each float is a segment
        public int SegmentByteLength => sizeof(float);

        public ImmutableFloatVector(IReadOnlyList<float> data)
        {
            Vector = data ?? throw new ArgumentNullException(nameof(data));
        }

        // Constructor from float[] for convenience, used by VectorMath.cs and potentially elsewhere
        public ImmutableFloatVector(float[] data)
            : this((IReadOnlyList<float>)data)
        {
        }

        public string GetBucketHash(int segments)
        {
            // Placeholder or simple implementation
            // Ensure segments is not larger than Vector.Count to avoid ArgumentOutOfRangeException in Take
            int segmentsToTake = Math.Min(segments, Vector.Count);
            return string.Join("-", Vector.Take(segmentsToTake).Select(f => f.ToString("F2")));
        }
    }
}
