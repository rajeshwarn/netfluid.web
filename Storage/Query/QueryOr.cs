﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class QueryOr : Query
    {
        public Query Left { get; private set; }
        public Query Right { get; private set; }

        public QueryOr(Query left, Query right)
            : base(null)
        {
            this.Left = left;
            this.Right = right;
        }

        // Never runs in AND/OR queries
        internal override IEnumerable<IndexNode> Execute(FluidDatabase engine, CollectionIndex index)
        {
            return null;
        }

        internal override IEnumerable<IndexNode> Run(FluidDatabase engine, CollectionPage col)
        {
            var left = this.Left.Run(engine, col);
            var right = this.Right.Run(engine, col);

            return left.Union(right, new IndexNodeComparer());
        }
    }
}
