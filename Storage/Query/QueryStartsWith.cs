using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class QueryStartsWith : Query
    {
        public string Value { get; private set; }

        public QueryStartsWith(string field, string value)
            : base(field)
        {
            this.Value = value;
        }

        internal override IEnumerable<IndexNode> Execute(FluidDatabase engine, CollectionIndex index)
        {
            return engine.Indexer.FindStarstWith(index, this.Value);
        }
    }
}
