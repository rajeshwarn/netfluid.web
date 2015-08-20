using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Netfluid.DB
{
    public interface IRepository<T> where T : new()
    {
        LiteDatabase Database { get; }
        string Name { get; }

        bool Any();
        bool Any(Query query);
        bool Any(Expression<Func<T, bool>> predicate);
        int Count();
        int Count(Query query);
        int Count(Expression<Func<T, bool>> predicate);
        int Delete(Query query);
        int Delete(Expression<Func<T, bool>> predicate);
        bool Delete(BsonValue id);
        bool Drop();
        bool DropIndex(string field);
        bool EnsureIndex(string field, IndexOptions options);
        bool EnsureIndex(string field, bool unique = false);
        bool EnsureIndex<K>(Expression<Func<T, K>> property, IndexOptions options);
        bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false);
        IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> FindAll();
        T FindById(BsonValue id);
        T FirstOrDefault(Expression<Func<T, bool>> predicate);
        T FirstOrDefault(Query query);
        IEnumerable<BsonDocument> GetIndexes();
        LiteCollection<T> Include(Action<T> action);
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
        bool Update(BsonValue id, T document);
    }
}