using System.Formats.Asn1;
using System.Text.Encodings.Web;

namespace BinaryVectorDB
{
    public record BinaryVector<T>
    {
        public uint[] Vector { get; init; }
        public T Value { get; init; }

        public BinaryVector(uint[] vector, T value)
        {
            Vector = vector;
            Value = value;
        }

        public BinaryVector(Span<uint> vector, T value)
        {
            Vector = vector.ToArray();
            Value = value;
        }

        public string ToBucketString(int bucketSize)
        {
            return string.Join(string.Empty, Vector.Take(bucketSize).Select(v => v.ToString("X8")));
        }
    }
}
