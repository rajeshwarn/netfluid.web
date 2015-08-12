﻿using Netfluid.DB;
using System;

namespace Netfluid
{
    internal class AutoId
    {
        /// <summary>
        /// Function to test if type is empty
        /// </summary>
        public Func<object, bool> IsEmpty { get; set; }

        /// <summary>
        /// Function that implements how generate a new Id for this type
        /// </summary>
        public Func<LiteCollection<BsonDocument>, object> NewId { get; set; }
    }
}
