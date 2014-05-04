using System;
using System.Collections.Generic;
using System.IO;


namespace NetFluid.Service
{
    [Serializable]
    public class Host
    {
        public string Name;
        public string Application;
        public List<string> Hosts;
        public string EndPoint;
        public bool Stopped;

        public Host()
        {
        }

        public Host(string name, string app, string host, string endpoint)
        {
            Name = name;
            Application = Path.GetFullPath(app);
            Hosts = new List<string> {host};

            EndPoint = endpoint;
            Stopped = false;
        }
    }
}
