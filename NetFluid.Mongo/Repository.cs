using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        private readonly PropertyInfo property;

        public Repository(string connection,string db)
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(x => x.Id).SetIdGenerator(StringObjectIdGenerator.Instance));
                typeof(T).Assembly.GetTypes().Where(x=>x.Inherit(typeof(T))).ForEach(derived =>
                {
                    cm.AddKnownType(derived);
                });
            });
            var client = new MongoClient(connection);
            database = client.GetServer().GetDatabase(db);
        }

        private MongoCollection<T> Collection
        {
            get { return database.GetCollection<T>(typeof (T).Name); }
        }

        private IQueryable<T> Queryable
        {
            get { return Collection.AsQueryable(); }
        }


        public T this[string id]
        {
            get
            {
                return Collection.FindOne(Query.EQ("_id",id));
            }
        }

        public IEnumerable<T> OfType(Type type)
        {
            return Collection.Find(Query.EQ("_t", type.Name));
        }

        public IEnumerable<T> OfType(string type)
        {
            return Collection.Find(Query.EQ("_t", type));
        }

        public void Save(IEnumerable<T> obj)
        {
            obj.ForEach(x=>Collection.Save(x));
        }

        public void Save(T obj)
        {
            Collection.Save(obj);
        }

        public void Remove(string id)
        {
            Collection.Remove(Query.EQ("_id", id));
        }

        public void Remove(T obj)
        {
            Collection.Remove(Query.EQ("_id",obj.Id));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Queryable.GetEnumerator();
        }

        public Expression Expression
        {
            get { return Queryable.Expression; }
            private set {  }
        }
        public Type ElementType
        {
            get { return Queryable.ElementType; }
            private set { }
        }
        public IQueryProvider Provider
        {
            get { return Queryable.Provider; }
            private set { }
        }
    }
}
