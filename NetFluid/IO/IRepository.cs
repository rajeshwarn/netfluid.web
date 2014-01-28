using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
