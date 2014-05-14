using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NetFluid.Mongo
{
    public interface MongoObject
    {
        string Id { get; set; }
    }
}
