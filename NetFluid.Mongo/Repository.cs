using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace NetFluid.Mongo
{
    public class Repository<T> : IQueryable<T> where T : MongoObject
    {
        private readonly MongoDatabase database;
        private readonly PropertyInfo property;

        public Repository(string connection,string db)
        {
            property = typeof (T).GetProperty("Id");

            if (property == null || property.PropertyType != typeof (string) || property.SetMethod == null)
                throw new Exception("Type " + typeof (T) + " must implement public string Id {get;set;} property");

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
                return Collection.FindOne(Query.EQ("_id", ObjectId.Parse(id)));
            }
        }

        public void Save(T obj)
        {
            Collection.Save(obj);
        }

        public void Remove(string id)
        {
            Collection.Remove(Query.EQ("_id", ObjectId.Parse(id)));
        }

        public void Remove(T obj)
        {
            Collection.Remove(Query.EQ("_id",ObjectId.Parse(property.GetValue(obj) as string)));
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
