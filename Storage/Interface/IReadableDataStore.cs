using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDB.Storage.Interface
{
    public interface IReadableDataStore<T>
    {
        public T Get(T query);
        public List<T> GetAll();
        public List<T> GetAll(T query);
    }
}
