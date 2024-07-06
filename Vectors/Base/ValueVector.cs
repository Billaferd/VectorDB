using System.Formats.Asn1;
using System.Text.Encodings.Web;
using VectorDB.Vector.Interface;

namespace Vector.Base
{
    public abstract record ValueVector<T1, T2>
            : FlatVector<T1> where T1 : struct
    {
        public T2 Value { get; init; }

        public ValueVector(T1[] vector, T2 value)
            : base(vector)
        {
            Value = value;
        }

        public ValueVector(Span<T1> vector, T2 value)
            : base(vector)
        {
            Value = value;
        }
    }
}
