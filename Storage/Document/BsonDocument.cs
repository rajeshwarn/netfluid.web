using NetFluid;
using System;
using System.Collections.Generic;

namespace FluidDB
{
    /// <summary>
    /// Represent a document schemeless to use in collections.
    /// </summary>
    public class BsonDocument : BsonObject, IDatabaseObject
    {
        public BsonDocument(): base()
        {
        }

        public BsonDocument(BsonValue value) : base(value.AsObject.RawValue)
        {
            if (!HasKey("Id")) throw new ArgumentException("BsonDocument must have an Id key");

            Id = this["Id"].RawValue.ToString();
            RemoveKey("Id");
        }

        public BsonDocument(Dictionary<string, object> obj) : base(obj)
        {
        }

        public string Id { get; set; }
    }
}
