
using System;
using MongoDB.Bson;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Serializable]
    public class CDN : MongoObject
    {
        public string Id { get; set; }
        public string Host;
        public string Path;
    }
}
