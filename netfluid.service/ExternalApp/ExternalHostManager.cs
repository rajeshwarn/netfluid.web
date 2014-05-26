using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("/host-ext")]
    public class ExternalHostManager:FluidPage
    {
        public static Repository<ExternalHost> ExternalHosts { get; private set; }
        private static ConcurrentDictionary<string, Process> Processes;

        public override void OnLoad()
        {
            ExternalHosts = new Repository<ExternalHost>("mongodb://localhost", "NetFluidService");
            Processes = new ConcurrentDictionary<string, Process>();

            if (!Directory.Exists("./External-App"))
                Directory.CreateDirectory("./External-App");

            ExternalHosts.ForEach(host =>
            {
                host.Hosts.ForEach(x => Engine.Cluster.AddFowarding(x, host.EndPoint));
                host.Start();
            });
        }

        static void Stop(ExternalHost host)
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
            foreach (var p in ExternalHosts)
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
            ExternalHosts.Remove(ExternalHosts[id]);
            return new RedirectResponse("/");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<ExternalHost>();
            ExternalHosts.Save(h);
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/start")]
        public IResponse Start(string id)
        {
            var host = ExternalHosts[id];

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
            Stop(ExternalHosts[id]);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ReStart(string id)
        {
            var host = ExternalHosts[id];
            
            Stop(host);

            if (host != null)
                host.Start();
            
            return new RedirectResponse("/");
        }
    }
}
