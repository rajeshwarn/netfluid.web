using System;

namespace NetFluid.Service.Forwarding
{
    [Serializable]
    public class Forwarding : IDatabaseObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string EndPoint { get; set; }
        public string[] Hosts { get; set; }
        public bool Enabled { get; set; }
    }
}
