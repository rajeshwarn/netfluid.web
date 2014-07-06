using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver.Linq;

namespace NetFluid.Mongo
{
    public class MemoryRepository<T>:Repository<T> where T : IDatabaseObject
    {
        IQueryable<T> values; 
 
        public MemoryRepository(string connection, string db) : base(connection, db)
        {
            values = Collection.AsQueryable().ToArray().AsQueryable();
        }

        public override IQueryable<T> Queryable
        {
            get { return values; }
        }

        public override Type ElementType
        {
            get { return values.ElementType; }
        }

        public override Expression Expression
        {
            get { return values.Expression; }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public override IQueryProvider Provider
        {
            get { return values.Provider; }
        }

        public override void Save(IEnumerable<T> obj)
        {
            base.Save(obj);
            values= Collection.AsQueryable().ToArray().AsQueryable();
        }

        public override void Save(T obj)
        {
            base.Save(obj);
            values = Collection.AsQueryable().ToArray().AsQueryable();
        }

        public override void Remove(T obj)
        {
            base.Remove(obj);
            values = Collection.AsQueryable().ToArray().AsQueryable();
        }

        public override void Remove(string id)
        {
            base.Remove(id);
            values = Collection.AsQueryable().ToArray().AsQueryable();
        }
    }
}
