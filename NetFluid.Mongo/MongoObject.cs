using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NetFluid.Mongo
{
    public interface MongoObject
    {
        [BsonId] ObjectId _id { get; set; }
    }
}
