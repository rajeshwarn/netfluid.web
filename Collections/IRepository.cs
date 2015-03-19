using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NetFluid.Collections
{
    public interface IRepository<T> : IQueryable<T>
    {
        IQueryable<T> Queryable { get; }
        T this[string id] { get; }
        IEnumerable<T> OfType(Type type);
        IEnumerable<T> OfType(string type);
        void Save(IEnumerable<T> obj);
        void Save(T obj);
        void Remove(string id);
        void Remove(T obj);
    }
}