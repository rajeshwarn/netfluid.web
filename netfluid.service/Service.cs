using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;


namespace NetFluid.Service
{
    class Service : ServiceBase
    {
        static ConcurrentDictionary<string,Host> _hosts;
        static ConcurrentDictionary<string, Process> _processes;
 
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
            get { return _hosts.Keys; }
        }

        public static Host ApplicationData(string name)
        {
            Host host = null;
            _hosts.TryGetValue(name, out host);
            return host;
        }

        public static Host Stop(string name)
        {
            Process process;
            Host host;

            if (_hosts.TryGetValue(name,out host) && _processes.TryGetValue(name, out process))
            {
                process.Kill();
                Start(host.Name);
            }
            return host;
        }

        public static Host Start(string name)
        {
            Host host;
            if (!_hosts.TryGetValue(name, out host))
                return null;

            var process = Process.Start("FluidPlayer.exe", host.Application);

            if (process == null) return host;
            
            process.Exited += (x, y) =>
            {
                if (!host.Stopped)
                {
                    Engine.Logger.Log(LogLevel.Error, "Host " + host.Name + " unexpected termination, restarting");
                    process = Process.Start("FluidPlayer.exe", host.Application);
                }
            };

            _processes.AddOrUpdate(name, process, (x, y) =>
            {
                try
                {
                    y.Kill();
                }
                catch (Exception)
                {
                }
                return process;
            });
            return host;
        }

        public static void ReStart(string name)
        {
            Stop(name);
            Start(name);
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

            if (File.Exists("hosts.xml"))
            {
                hosts = hosts.FromXML(File.ReadAllText("hosts.xml"));
            }

            _hosts = new ConcurrentDictionary<string, Host>();
            _processes = new ConcurrentDictionary<string, Process>();

            hosts.ForEach(host =>
            {
                _hosts.TryAdd(host.Name,host);
                host.Hosts.ForEach(x =>Engine.Cluster.AddFowarding(x,host.EndPoint));
                Start(host.Name);
            });

            Engine.Start();
        }

        /// <SUMMARY>
        /// Stop this service.
        /// </SUMMARY>
        protected override void OnStop()
        {
            Engine.Logger.Log(LogLevel.Warning,"NetFluid Service is stopping");
            foreach (var p in _hosts)
            {
                try
                {
                    Stop(p.Key);
                }
                catch (Exception exception)
                {
                    Engine.Logger.Log(LogLevel.Exception,"Error stopping "+p.Key,exception);
                }
            }
        }
    }
}
