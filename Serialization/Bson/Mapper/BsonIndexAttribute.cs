
using System;

namespace Netfluid.DB
{
    /// <summary>
    /// Add an index in this entity property.
    /// </summary>
    public class BsonIndexAttribute : Attribute
    {
        public IndexOptions Options { get; private set; }

        public BsonIndexAttribute()
            : this (new IndexOptions())
        {
        }

        public BsonIndexAttribute(bool unique)
            : this(new IndexOptions { Unique = unique })
        {
        }

        public BsonIndexAttribute(IndexOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            this.Options = options;
        }
    }
}