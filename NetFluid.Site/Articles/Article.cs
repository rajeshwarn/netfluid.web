using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace NetFluidService
{
    [Serializable]
    public class Article
    {
        [NonSerialized] private static readonly MongoDatabase db;

        static Article()
        {
            var client = new MongoClient("mongodb://localhost");
            var server = client.GetServer();
            db = server.GetDatabase("netfluid");
        }

        [BsonId]
        public ObjectId _id { get; set; }

        public bool Approved { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Category { get; set; }
        public string Abstract { get; set; }
        public string Author { get; set; }
        public string Body { get; set; }
        public DateTime DateTime { get; set; }
        public uint Views { get; set; }
        public Attachment[] Attachment { get; set; }

        public static IEnumerable<Article> News
        {
            get
            {
                var sbb = new SortByBuilder();
                sbb.Descending("DateTime");
                return Collection.Find(Query.And(Query.EQ("Approved", true), Query.EQ("Category", "Discover"))).SetSortOrder(sbb).SetLimit(7);
            }
        }

        public static IEnumerable<Article> Documentation
        {
            get
            {
                var sbb = new SortByBuilder();
                sbb.Descending("DateTime");
                return Collection.Find(Query.And(Query.EQ("Approved", true), Query.EQ("Category", "Documentation"))).SetSortOrder(sbb);
            }
        }

        private static MongoCollection<Article> Collection
        {
            get { return db.GetCollection<Article>("Article"); }
        }

        public static void Save(Article article)
        {
            Collection.Save(article);
        }

        public static void Delete(Article article)
        {
            Collection.Remove(Query.EQ("Link", article.Link));
        }

        public static IEnumerable<Article> All()
        {
            return Collection.FindAll();
        }

        public static IEnumerable<Article> ByCategory(string category)
        {
            return Collection.Find(Query.And(Query.EQ("Category", category), Query.EQ("Approved",true)));
        }

        public static Article Parse(string link)
        {
            return Collection.FindOne(Query.EQ("Link", link));
        }
    }
}