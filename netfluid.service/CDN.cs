﻿
using System;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Serializable]
    public class CDN:MongoObject
    {
        public string Host;
        public string Path;
    }
}
