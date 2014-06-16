using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace NetFluid.Mongo
{
    public class Repository<T> : IQueryable<T> where T : IDatabaseObject
    {
        private readonly MongoDatabase database;

        public Repository(string connection,string db)
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(x => x.Id).SetIdGenerator(StringObjectIdGenerator.Instance));
                typeof(T).Assembly.GetTypes().Where(x=>x.Inherit(typeof(T))).ForEach(cm.AddKnownType);
            });
            var client = new MongoClient(connection);
            database = client.GetServer().GetDatabase(db);

            Collection = database.GetCollection<T>(typeof(T).Name);
        }

        protected readonly MongoCollection<T> Collection;

        protected virtual IQueryable<T> Queryable
        {
            get { return Collection.AsQueryable(); }
        }

        public virtual T this[string id]
        {
            get
            {
                return Collection.FindOne(Query.EQ("_id",id));
            }
        }

        public virtual IEnumerable<T> OfType(Type type)
        {
            return Collection.Find(Query.EQ("_t", type.Name));
        }

        public virtual IEnumerable<T> OfType(string type)
        {
            return Collection.Find(Query.EQ("_t", type));
        }

        public virtual void Save(IEnumerable<T> obj)
        {
            obj.ForEach(x=>Collection.Save(x));
        }

        public virtual void Save(T obj)
        {
            Collection.Save(obj);
        }

        public virtual void Remove(string id)
        {
            Collection.Remove(Query.EQ("_id", id));
        }

        public virtual void Remove(T obj)
        {
            Collection.Remove(Query.EQ("_id",obj.Id));
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return Queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Queryable.GetEnumerator();
        }

        public virtual Expression Expression
        {
            get { return Queryable.Expression; }
            private set {  }
        }
        public virtual Type ElementType
        {
            get { return Queryable.ElementType; }
            private set { }
        }
        public virtual IQueryProvider Provider
        {
            get { return Queryable.Provider; }
            private set { }
        }
    }
}
