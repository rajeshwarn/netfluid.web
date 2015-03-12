using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class QueryAll : Query
    {
        public QueryAll()
            : base("Id")
        {
        }

        public QueryAll(string field)
            : base(field)
        {
        }

        internal override IEnumerable<IndexNode> Execute(FluidDatabase engine, CollectionIndex index)
        {
            return engine.Indexer.FindAll(index);
        }
    }
}
