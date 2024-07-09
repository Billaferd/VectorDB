using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector.Base;

namespace VectorDB.Vector
{
    public record ImmutableFloatVector
        : ImmutableValuelessVector<float>
    {
        public ImmutableFloatVector(float[] vector)
            : base(vector) { }

        public ImmutableFloatVector(Span<float> vector)
            : base(vector) { }
    }
}
