
using System;

namespace NetFluid.Service
{
    [Serializable]
    public class CDN : IDatabaseObject
    {
        public string Id { get; set; }
        public string Host;
        public string Path;
    }
}
