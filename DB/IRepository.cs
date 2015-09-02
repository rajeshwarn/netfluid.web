using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Netfluid.DB
{
    public interface IRepository<T> where T : new()
    {
        string Name { get; }

        bool Any();
        bool Any(Expression<Func<T, bool>> predicate);

        int Count();
        int Count(Expression<Func<T, bool>> predicate);

        int Delete(Expression<Func<T, bool>> predicate);

        bool Drop();
        bool DropIndex(string field);

        bool EnsureIndex(string field);

        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);

        IEnumerable<T> FindAll();

        T FirstOrDefault(Expression<Func<T, bool>> predicate);

        void Insert(T document);
        void Insert(IEnumerable<T> docs);

        bool Update(T document);
    }
}