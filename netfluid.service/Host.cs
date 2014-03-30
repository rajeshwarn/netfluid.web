using System;
using System.Collections.Generic;
using System.IO;


namespace NetFluid.Service
{
    [Serializable]
    public class Host
    {
        public string Application;
        public List<string> Hosts;
        public string EndPoint;

        public Host()
        {
        }

        public Host(string app, string name, string endpoint)
        {
            Application = Path.GetFullPath(app);
            Hosts = new List<string> {name};

            EndPoint = endpoint;
        }
    }
}
