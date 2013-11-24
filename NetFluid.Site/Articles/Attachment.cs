using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NetFluidService
{
    public class Attachment
    {
        [BsonId]
        public ObjectId _id { get; set; }

        public string FileName { get; set; }

        public string Name { get; set; }
    }
}
