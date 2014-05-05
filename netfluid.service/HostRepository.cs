using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NetFluid.Collections;

namespace NetFluid.Service
{
    public class HostRepository
    {
        static readonly XMLRepository<Host> _hosts;
        private static readonly ConcurrentDictionary<string, Process> _processes;

        static HostRepository()
        {
            _hosts = new XMLRepository<Host>("hosts.xml");
            _processes = new ConcurrentDictionary<string, Process>();


            if (!Directory.Exists("./Hosting"))
                Directory.CreateDirectory("./Hosting");

        }

        public static void Start()
        {
            _hosts.ForEach(host =>
            {
                host.Hosts.ForEach(x => Engine.Cluster.AddFowarding(x, host.EndPoint));
                Start(host.Id);
            });
        }

        public static void Stop()
        {
            foreach (var p in _hosts)
            {
                try
                {
                    Stop(p.Name);
                }
                catch (Exception exception)
                {
                    Engine.Logger.Log(LogLevel.Exception, "Error stopping " + p.Name, exception);
                }
            }
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

        public static Host Stop(string id)
        {
            Process process;
            var host = Get(id);

            if (host != null && _processes.TryGetValue(id, out process))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                }
                _processes.TryRemove(id, out process);
            }
            return host;
        }

        public static Host Start(string id)
        {
            var host = Get(id);

            if (host == null)
                return null;
            try
            {
                var info = new ProcessStartInfo()
                {
                    FileName = "FluidPlayer.exe",
                    Arguments = host.Application,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };

                var k = host.Username ?? "";

                if (host.Username != null && host.Password != null)
                {
                    info.UserName = host.Username;
                    info.Password = Security.Secure(host.Password);
                }

                var process = Process.Start(info);

                process.Exited += (x, y) =>
                {
                    if (host.Enabled)
                    {
                        Engine.Logger.Log(LogLevel.Error, "Host " + host.Name + " unexpected termination, restarting");
                        Start(id);
                    }
                };

                _processes.AddOrUpdate(id, process, (x, y) =>
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

        public static void ReStart(string id)
        {
            Stop(id);
            Start(id);
        }

        public static Host Get(string id)
        {
            return _hosts.FirstOrDefault(x => x.Id == id);
        }

        public static Host Update(string id, string name, string application, string host, string endpoint, bool enabled, string username, string password)
        {
            var h = Get(id);

            if (h != null)
            {
                h.Name = name;
                h.Application = application;
                h.Hosts = new List<string>(host.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                h.EndPoint = endpoint;
                h.Enabled = enabled;
                h.Username = username;
                h.Password = password;
            }

            if (enabled)
                Start(id);

            _hosts.Save();

            return h;
        }

        public static Host Add(string name, string application, string host, string endpoint, bool enabled, string username, string password)
        {
            var n = new Host
            {
                Id = Security.UID(),
                Name = name,
                Application = application,
                Hosts = new List<string>(host.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)),
                Enabled = enabled,
                EndPoint = endpoint,
                Username = username,
                Password = password
            };

            if(!enabled)
                Stop(n.Id);

            _hosts.Save();

            return n;
        }

        public static void Delete(string id)
        {
            _hosts.Remove(x=>x.Id==id);
        }
    }
}
