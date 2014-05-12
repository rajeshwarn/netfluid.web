
using System;
using MongoDB.Bson;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Serializable]
    public class CDN : MongoObject
    {
        public ObjectId _id { get; set; }
        public string Host;
        public string Path;
    }
}
