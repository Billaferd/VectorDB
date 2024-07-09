using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector.Base;

namespace VectorDB.Vector
{
    public record ImmutableByteVector
        : ImmutableValuelessVector<byte>
    {

        public ImmutableByteVector(byte[] vector)
            : base(vector) { }

        public ImmutableByteVector(Span<byte> vector)
            : base(vector) { }

    }
}
