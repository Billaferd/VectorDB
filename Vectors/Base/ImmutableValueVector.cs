using System.Formats.Asn1;
using System.Text.Encodings.Web;
using VectorDB.Vector.Interface;

namespace Vector.Base
{
    public abstract record ImmutableValueVector<T1, T2>
            : ImmutableValuelessVector<T1> where T1 : struct
    {
        public T2 Value { get; init; }

        public ImmutableValueVector(T1[] vector, T2 value)
            : base(vector)
        {
            Value = value;
        }

        public ImmutableValueVector(Span<T1> vector, T2 value)
            : base(vector)
        {
            Value = value;
        }
    }
}
