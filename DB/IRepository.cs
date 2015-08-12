using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Netfluid.Mongo;

namespace Netfluid.DB
{
    public interface IRepository<T> where T : new()
    {
        string Name { get; }
        int Count();
        int Count(Expression<Func<T, bool>> predicate);
        int Delete(Expression<Func<T, bool>> predicate);
        bool Drop();
        bool DropIndex(string field);
        bool Any();
        bool EnsureIndex(string field, bool unique = false);
        bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false);
        bool Exists(Expression<Func<T, bool>> predicate);
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> FindAll();
        T FindOne(Expression<Func<T, bool>> predicate);
        IEnumerable<BsonDocument> GetIndexes();
        BsonValue Insert(T document);
        int Insert(IEnumerable<T> docs);
        int InsertBulk(IEnumerable<T> docs, int buffer = 32768);
        BsonValue Max();
        BsonValue Max(string field);
        BsonValue Max<K>(Expression<Func<T, K>> property);
        BsonValue Min();
        BsonValue Min(string field);
        BsonValue Min<K>(Expression<Func<T, K>> property);
        bool Update(T document);
        User FirstOrDefault(Func<T, bool> p);
        bool Any(Func<T, bool> p);
    }
}