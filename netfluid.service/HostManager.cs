using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [VirtualHost("wwww")]
    [Route("/host")]
    public class HostManager:FluidPage
    {
        public static Repository<Host> Hosts { get; private set; }
        private static readonly ConcurrentDictionary<string, Process> Processes;

        static HostManager()
        {
            Hosts = new Repository<Host>("mongodb://localhost", "NetFluidService");
            Processes = new ConcurrentDictionary<string, Process>();
        }

        public static void Start()
        {
            Hosts.ForEach(host =>
            {
                host.Hosts.ForEach(x => Engine.Cluster.AddFowarding(x, host.EndPoint));
                host.Start();
            });
        }

        static void Stop(Host host)
        {
            try
            {
                if (host == null) return;
                var key = host.Id;
                Process process;
                if (Processes.TryGetValue(key, out process))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    Processes.TryRemove(key, out process);
                }
            }
            catch (Exception exception)
            {
                Engine.Logger.Log(LogLevel.Exception, "Error stopping " + host.Name, exception);
            }
        }

        public static void Stop()
        {
            foreach (var p in Hosts)
            {
                Stop(p);
            }
        }

        public static bool IsUp(string id)
        {
            Process p;
            return Processes.TryGetValue(id, out p) && !p.HasExited;
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            Hosts.Remove(Hosts[id]);
            return new RedirectResponse("/");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<Host>();
            Hosts.Save(h);
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/start")]
        public IResponse Start(string id)
        {
            var host = Hosts[id];

            if (host == null)
                return null;

            var process = host.Start();

            Processes.AddOrUpdate(id, process, (x, y) =>
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

            return new RedirectResponse("/");
        }
        
        [ParametrizedRoute("/stop")]
        public IResponse Stop(string id)
        {
            Stop(Hosts[id]);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ReStart(string id)
        {
            var host = Hosts[id];
            
            Stop(host);

            if (host != null)
                host.Start();
            
            return new RedirectResponse("/");
        }
    }
}
