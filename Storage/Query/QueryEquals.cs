﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class QueryEquals : Query
    {
        public object Value { get; private set; }

        public QueryEquals(string field, object value)
            : base(field)
        {
            this.Value = value;
        }

        internal override IEnumerable<IndexNode> Execute(FluidDatabase engine, CollectionIndex index)
        {
            return engine.Indexer.FindEquals(index, this.Value);
        }
    }
}
