using System.Collections.Generic;

namespace NetFluid.IO
{
    public interface IRepository<T>
    {
        void Create(T element);
        IEnumerable<T> Read();
        void Update(T elem);
        void Delete(T elem);
    }
}
