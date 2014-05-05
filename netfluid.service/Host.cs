using System;
using System.Collections.Generic;
using System.IO;


namespace NetFluid.Service
{
    [Serializable]
    public class Host
    {
        public string Id;
        public string Name;
        public string Application;
        public List<string> Hosts;
        public string EndPoint;
        public bool Enabled;
    }
}
