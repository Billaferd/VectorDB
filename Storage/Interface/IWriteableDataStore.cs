using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Interface
{
    public interface IWriteableDataStore<T>
    {
        public void Add(T query);
        public void Update(T query);
        public void Delete(T query);
        public void DeleteAll(T query);
        public void Truncate();
    }
}
