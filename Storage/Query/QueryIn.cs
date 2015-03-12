using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class QueryIn : Query
    {
        public object[] Values { get; private set; }

        public QueryIn(string field, object[] values)
            : base(field)
        {
            this.Values = values;
        }

        internal override IEnumerable<IndexNode> Execute(FluidDatabase engine, CollectionIndex index)
        {
            return engine.Indexer.FindIn(index, this.Values);
        }
    }
}
