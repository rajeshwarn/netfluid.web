﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class QueryNot : Query
    {
        public object Value { get; private set; }

        public QueryNot(string field, object value)
            : base(field)
        {
            this.Value = value;
        }

        internal override IEnumerable<IndexNode> Execute(FluidDatabase engine, CollectionIndex index)
        {
            return engine.Indexer.FindAll(index).Where(x => x.Key.CompareTo(new IndexKey(this.Value)) != 0);
        }
    }
}
