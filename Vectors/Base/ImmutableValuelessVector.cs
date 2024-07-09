using VectorDB.Vector.Interface;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Vector.Base
{
    public abstract record ImmutableValuelessVector<T1>
        : IImmutableVector<T1> where T1 : struct
    {
        public IReadOnlyList<T1> Vector { get; init; }

        public ImmutableValuelessVector(T1[] vector)
        {
            Vector = ImmutableArray.Create(vector);
            SegmentByteLength = Marshal.SizeOf(typeof(T1));
            SegmentBitLength = SegmentByteLength * 8;
            VectorByteLength = SegmentByteLength * Vector.Length;
            VectorBitLength = VectorByteLength * 8;
        }

        public ImmutableValuelessVector(Span<T1> vector)
            : this(vector.ToArray()) {}

        public int VectorBitLength { get; init; }

        public int VectorByteLength { get; init; }

        public int SegmentBitLength { get; init; }

        public int SegmentByteLength { get; init; }

        /// <summary>
        /// Converts a vector to a string for simple bucketing.
        /// </summary>
        /// <param name="stringSize">The size of the string for bucketing.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the stringSize parameter is too big or too small.</exception>
        public virtual string GetBucketHash(int stringSize)
        {
            string bucketString = string.Empty;

            if (stringSize > 0 && stringSize <= VectorByteLength)
            {
                byte[] rawData = new byte[stringSize];

                Buffer.BlockCopy(Vector.ToArray(), 0, rawData, 0, stringSize);

                rawData = rawData.Select(datum => (byte)(datum % 26 + 65)).ToArray();

                bucketString = System.Text.Encoding.UTF8.GetString(rawData);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(stringSize), $"Parameter {nameof(stringSize)} must be larger than 0 and less then or equal to {VectorByteLength}.");
            }

            return bucketString;
        }
    }
}
