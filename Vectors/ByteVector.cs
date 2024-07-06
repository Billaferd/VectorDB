using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector.Base;

namespace VectorDB.Vector
{
    public record ByteVector
        : FlatVector<byte>
    {

        public ByteVector(byte[] vector)
            : base(vector) { }

        public ByteVector(Span<byte> vector)
            : base(vector) { }

    }
}
