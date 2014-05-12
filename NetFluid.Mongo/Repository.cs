﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace NetFluid.Mongo
{
    public class Repository<T>:IQueryable<T> where T:MongoObject
    {
        private readonly IQueryable<T> queryable; 
        private readonly MongoDatabase database;

        public Repository(string connection,string db) 
        {
            var client = new MongoClient(connection);
            database = client.GetServer().GetDatabase(db);
            queryable= Collection.AsQueryable();
        }

        private MongoCollection<T> Collection
        {
            get { return database.GetCollection<T>(typeof (T).Name); }
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
            Collection.Remove(Query.EQ("_id",ObjectId.Parse(obj.Id)));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return queryable.GetEnumerator();
        }

        public Expression Expression
        {
            get { return queryable.Expression; }
            private set {  }
        }
        public Type ElementType
        {
            get { return queryable.ElementType; }
            private set { }
        }
        public IQueryProvider Provider
        {
            get { return queryable.Provider; }
            private set { }
        }
    }
}
