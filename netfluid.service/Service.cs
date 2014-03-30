using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetFluid;
using System.IO;
using System.Reflection;
using System.ServiceProcess;


namespace NetFluid.Service
{
    class Service : ServiceBase
    {
        private List<Process> processes;
 
        static void Main(string[] args)
        {
            if (args!=null && args.Length>=1 && args[0]=="debug")
            {
               (new Service()).OnStart(null);
                Console.ReadLine();
            }
            else
            {
                Run(new Service());
            }

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

            processes = new List<Process>();

            hosts.ForEach(host =>
            {
                processes.Add(Process.Start("FluidPlayer.exe", host.Application));
                host.Hosts.ForEach(x =>Engine.Cluster.AddFowarding(x,host.EndPoint));
            });


            if (!Directory.Exists("./Plugin"))
                Directory.CreateDirectory("./Plugin");

            foreach (var plugin in Directory.GetFiles("./Plugin","*.dll",SearchOption.AllDirectories))
            {
                Engine.Load(Assembly.LoadFile(plugin));
            }

            Engine.Start();
        }

        /// <SUMMARY>
        /// Stop this service.
        /// </SUMMARY>
        protected override void OnStop()
        {
            foreach (var p in processes)
            {
                try
                {
                    p.Kill();
                }
                catch (Exception exception)
                {
                    File.WriteAllText(Security.UID(),exception.Message);
                }
            }
        }
    }
}
