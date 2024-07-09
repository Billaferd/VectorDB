using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Vector.Interface
{
    public interface IMutableVector<T1>
        : IVector<T1> where T1 : struct
    {
        new public T1[] Vector { get; init; }
    }
}
