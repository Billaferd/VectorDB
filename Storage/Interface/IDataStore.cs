using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VectorDB.Storage.Interface;

namespace Storage.Interface
{
    public interface IDataStore<T>
        : IReadableDataStore<T>, IWriteableDataStore<T>
    {
    }
}
