using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace NetFluid.Site.VOIP
{
    public class VoipUser
    {
        static readonly MongoDatabase db;

        static VoipUser()
        {
            var client = new MongoClient("mongodb://localhost");
            var server = client.GetServer();
            db = server.GetDatabase("nfvoip");
        }

        public static MongoCollection<VoipUser> Collection
        {
            get { return db.GetCollection<VoipUser>("VoipUser"); }
        }

        [BsonId]
        public ObjectId _id { get; set; }

        public string PeerId { get; set; }

        public DateTime LastLogin { get; set; }

        public static IEnumerable<VoipUser> All
        {
            get { return Collection.FindAll(); }
        }

        public static void Save(VoipUser user)
        {
            Collection.Save(user);
        }
    }
}
