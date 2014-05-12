
using System;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Serializable]
    public class CDN
    {
        public string Id { get; set; }
        public string Host;
        public string Path;
    }
}
