using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using NetFluid.Collections;


namespace NetFluid.Service
{
    public class Service : ServiceBase
    {
        static XMLRepository<Host> _hosts;
        static ConcurrentDictionary<string, Process> _processes;
 
        static void Main(string[] args)
        {
            if (args!=null && args.Length>=1 && args[0]=="debug")
            {
               (new Service()).OnStart(null);
               Engine.DevMode = true;
                Console.ReadLine();
                return;
            }
            Run(new Service());
        }

        public static bool IsUp(string name)
        {
            Process p;
            return _processes.TryGetValue(name, out p) && !p.HasExited;
        }

        public static IEnumerable<Host> Applications
        {
            get { return _hosts; }
        }

        public static Host Stop(string name)
        {
            Process process;
            var host = _hosts.FirstOrDefault(x => x.Name == name);

            if (host!=null && _processes.TryGetValue(name, out process))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                }
                _processes.TryRemove(name, out process);
            }
            return host;
        }

        public static Host Start(string name)
        {
            Host host = _hosts.FirstOrDefault(x => x.Name == name);
            if (host==null)
                return null;
            try
            {
                var process = Process.Start("FluidPlayer.exe", host.Application);

                process.Exited += (x, y) =>
                {
                    if (host.Enabled)
                    {
                        Engine.Logger.Log(LogLevel.Error, "Host " + host.Name + " unexpected termination, restarting");
                        Start(name);
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
            }
            catch (Exception)
            {

            }
            return host;
        }

        public static void ReStart(string name)
        {
            Stop(name);
            Start(name);
        }

        public static void Add(string name, string application, string host, string endpoint, bool enabled)
        {
            var n = new Host
            {
                Id = Security.UID(),
                Name = name,
                Application = application,
                Hosts = new List<string>(host.Split(new []{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)),
                Enabled = enabled,
                EndPoint = endpoint
            };
            Stop(name);
            _hosts.Remove(x=>x.Name==name);
            _hosts.Add(n);
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

            _hosts = new XMLRepository<Host>("hosts.xml");
            _processes = new ConcurrentDictionary<string, Process>();

            _hosts.ForEach(host =>
            {
                host.Hosts.ForEach(x =>Engine.Cluster.AddFowarding(x,host.EndPoint));
                Start(host.Name);
            });

            Engine.Start();

            //Engine.SetController(x => new FluidTemplate("./UI/index.html"),"Update in progress");
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
                    Stop(p.Name);
                }
                catch (Exception exception)
                {
                    Engine.Logger.Log(LogLevel.Exception,"Error stopping "+p.Name,exception);
                }
            }
        }
    }
}
