using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector.Base;

namespace VectorDB.Vector
{
    public record FloatVector
        : FlatVector<float>
    {
        public FloatVector(float[] vector)
            : base(vector) { }

        public FloatVector(Span<float> vector)
            : base(vector) { }
    }
}
