using System.Collections.Generic;
using System.Linq;
using Netfluid.Bson;

namespace Netfluid.DB
{
    /// <summary>
    /// Contains query do not work with index, only full scan
    /// </summary>
    class QueryContains : Query
    {
        private BsonValue _value;

        public QueryContains(string field, BsonValue value)
            : base(field)
        {
            _value = value;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            var v = _value.Normalize(index.Options);

            return indexer.FindAll(index, Query.Ascending).Where(x => x.Key.AsString.Contains(v));
        }
    }
}
