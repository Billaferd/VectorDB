using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Vector.Interface
{
    public interface IImmutableVector<T1>
        : IVector<T1> where T1 : struct
    {}
}
