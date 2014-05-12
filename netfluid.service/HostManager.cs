using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using MongoDB.Bson;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("/host")]
    public class HostManager:FluidPage
    {
        private static readonly Repository<Host> Hosts;
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
                Process process;
                if (host != null && Processes.TryGetValue(host.Id, out process))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    Processes.TryRemove(host.Id, out process);
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

        public static bool IsUp(ObjectId id)
        {
            Process p;
            return Processes.TryGetValue(id, out p) && !p.HasExited;
        }

        public static IEnumerable<Host> Applications
        {
            get { return Hosts; }
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

            Processes.AddOrUpdate(host.Id, process, (x, y) =>
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
