using System;
using System.Collections.Generic;
using System.IO;
using System.Security;


namespace NetFluid.Service
{
    [Serializable]
    public class Host
    {
        public string Id;
        public string Name;
        public string Application;
        public string[] Hosts;
        public string EndPoint;
        public bool Enabled;

        public string Username;
        public string Password;
    }
}
