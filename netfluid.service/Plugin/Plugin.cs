using System;

namespace NetFluid.Service
{
    [Serializable]
    public class Plugin : IDatabaseObject
    {
        public string Id { get; set; }

        public string Application;
        public bool Enabled;
        public string[] Hosts;
        public string Name;
    }
}