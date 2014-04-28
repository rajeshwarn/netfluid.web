using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceProcess;


namespace NetFluid.Service
{
    class Service : ServiceBase
    {
        static ConcurrentDictionary<string,Host> processes;
 
        static void Main(string[] args)
        {
            if (args!=null && args.Length>=1 && args[0]=="debug")
            {
               (new Service()).OnStart(null);
                Console.ReadLine();
                return;
            }
            Run(new Service());
        }

        public static IEnumerable<string> Applications
        {
            get { return processes.Keys; }
        }

        public static Host ApplicationData(string name)
        {
            Host host = null;
            processes.TryGetValue(name, out host);
            return host;
        }

        protected override void OnStart(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            #region LOAD CONFIGURATION
            if (!Engine.LoadAppConfiguration())
            {
                Engine.Interfaces.AddAllAddresses();
                Engine.AddPublicFolder("/", "./Public", true);
                Engine.Interfaces.AddInterface("127.0.0.1", 80);
                Engine.Interfaces.AddInterface("127.0.0.1", 8000);
            }
            #endregion

            if (!Directory.Exists("./Hosting"))
                Directory.CreateDirectory("./Hosting");

            var hosts = new List<Host>();

            if (!File.Exists("hosts.xml"))
            {
                var dhost = new Host("./Hosting/NetFluid.Site/NetFluid.Site.dll","www.netfluid.org","localhost:8080");
                hosts.Add(dhost);
                File.WriteAllText("hosts.xml",hosts.ToXML());
            }
            else
            {
                hosts = hosts.FromXML(File.ReadAllText("hosts.xml"));
            }

            processes = new ConcurrentDictionary<string, Host>();

            hosts.ForEach(host =>
            {
                processes.TryAdd(host.Name,host);
                host.Start();
                host.Hosts.ForEach(x =>Engine.Cluster.AddFowarding(x,host.EndPoint));
            });

            Engine.Start();
        }

        /// <SUMMARY>
        /// Stop this service.
        /// </SUMMARY>
        protected override void OnStop()
        {
            Engine.Logger.Log(LogLevel.Warning,"NetFluid Service is stopping");
            foreach (var p in processes)
            {
                try
                {
                    p.Value.Kill();
                }
                catch (Exception exception)
                {
                    Engine.Logger.Log(LogLevel.Exception,"Error stopping "+p.Key,exception);
                }
            }
        }
    }
}
