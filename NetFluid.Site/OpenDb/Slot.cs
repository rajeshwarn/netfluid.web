using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NetFluid.Site.OpenDb
{
    public class Slot
    {
        [BsonId]
        public ObjectId _id { get; set; }

        public string OpenId { get; set; }

        public BsonDocument Document { get; set; }

        public DateTime Timestamp { get; set; }

        public string Token { get; set; }
    }
}
