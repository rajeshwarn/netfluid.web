using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("host")]
    public class HostManager:FluidPage
    {
        private static readonly Repository<Host> hosts;
        private static readonly ConcurrentDictionary<ObjectId, Process> _processes;

        static HostManager()
        {
            hosts = new Repository<Host>("mongodb://localhost", "NetFluidService");
            _processes = new ConcurrentDictionary<ObjectId, Process>();
        }

        public static void Start()
        {
            hosts.ForEach(host =>
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
                if (host != null && _processes.TryGetValue(host.Id, out process))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }
                    _processes.TryRemove(host.Id, out process);
                }
            }
            catch (Exception exception)
            {
                Engine.Logger.Log(LogLevel.Exception, "Error stopping " + host.Name, exception);
            }
        }

        public static void Stop()
        {
            foreach (var p in hosts)
            {
                Stop(p);
            }
        }

        public static bool IsUp(ObjectId id)
        {
            Process p;
            return _processes.TryGetValue(id, out p) && !p.HasExited;
        }

        public static IEnumerable<Host> Applications
        {
            get { return hosts; }
        }

        [Route("/")]
        public IResponse Home()
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            var host = Context.Request.Values.ToObject<Host>(); 

            switch (Request.HttpMethod)
            {
                case "POST":
                    return Add(host);
                case "PUT":
                    return Update(host);
                case "DELETE":
                    return Delete(host);
            }
            return new FluidTemplate("./UI/admin.html");
        }

        public static IResponse Delete(Host host)
        {
            hosts.Remove(host);
            return new RedirectResponse("/");
        }

        public static IResponse Update(Host host)
        {
            hosts.Save(host);
            return new RedirectResponse("/");
        }

        public static IResponse Add(Host host)
        {
            hosts.Save(host);
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/start")]
        public IResponse Start(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            var host = hosts[id];

            if (host == null)
                return null;

            var process = host.Start();

            _processes.AddOrUpdate(host.Id, process, (x, y) =>
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
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");
            
            Stop(hosts[id]);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ReStart(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            var host = hosts[id];
            
            Stop(host);

            if (host != null)
                host.Start();
            
            return new RedirectResponse("/");
        }
    }
}
