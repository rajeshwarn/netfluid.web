
using System;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Serializable]
    public class CDN : IDatabaseObject
    {
        public string Id { get; set; }
        public string ExternalHost;
        public string Path;
    }
}
