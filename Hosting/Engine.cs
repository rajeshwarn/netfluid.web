// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Security.Permissions;
using System.Security.Principal;

namespace Netfluid
{
    /// <summary>
    /// Main class of Netfluid framework
    /// </summary>
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public static class Engine
    {
        private static readonly Dictionary<string, Host> _hosts;
        static HttpListener listener;

        static Engine()
        {
            DefaultHost = new Host("default");
            _hosts = new Dictionary<string, Host>();
            _hosts.Add("__default", DefaultHost);


            AppDomain.CurrentDomain.UnhandledException += (s, e) => 
            {
                if (e.IsTerminating)
                    Logger.Log("Unhandled fatal exception occurred.", e.ExceptionObject as Exception);
                else
                    Logger.Log("Unhandled exception occurred.", e.ExceptionObject as Exception);
            };
            
            Logger = new Logger();

            var max = Environment.ProcessorCount * 250;
            var min = Environment.ProcessorCount * 10;
            ThreadPool.SetMinThreads(min,max);
            ThreadPool.SetMaxThreads(max, max);

            ThreadPool.GetMinThreads(out min,out max);
            Logger.Log("threadpool size " + min + " " + max);

            listener = new HttpListener();
        }



        public static IEnumerable<string> Prefixes
        {
            get
            {
                return listener.Prefixes.Select(x=>x.ToString());
            }
        }

        /// <summary>
        /// Main of host of the apllication. Any request not handled by virtual hosts will be handled by this one.
        /// </summary>
        public static readonly Host DefaultHost;
        public static bool ShowException;

        /// <summary>
        /// Rewritable log manager
        /// </summary>
        public static ILogger Logger { get; set; }

        /// <summary>
        /// Currently running virtual hosts managers (reversed proxy excluded)
        /// </summary>
        public static IEnumerable<Host> Hosts
        {
            get { return _hosts.Values; }
        }

        /// <summary>
        /// If true log message and request serving flow are shown on the console
        /// </summary>
        public static bool DevMode { get; set; }

        /// <summary>
        /// Check if the program is running as administrator
        /// </summary>
        /// <returns></returns>
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Return the host manager from the host name 
        /// </summary>
        /// <param name="name">name of the host (ex: www.netfluid.org)</param>
        /// <returns>virtual host manager</returns>
        public static Host Host(string host)
        {
            if (string.IsNullOrEmpty(host))
                return DefaultHost;

            Host h;
            if (_hosts.TryGetValue(host, out h))
                return h;

            h=new Host(host);
            _hosts.Add(host, h);

            return h;
        }

        public static void Serve(Context cnt)
        {
            if (DevMode)
                Console.WriteLine("Serving " + cnt.Request.Url);

            Host host;
            if (_hosts.TryGetValue(cnt.Request.Url.Host, out host))
            {
                if (DevMode)
                    Console.WriteLine(cnt.Request.Url + " - Using host " + cnt.Request.Url.Host);

                host.Serve(cnt);
            }
            else
            {
                if (DevMode)
                    Console.WriteLine(cnt.Request.Url + " - Using default web application");

                DefaultHost.Serve(cnt);
            }
        }


        /// <summary>
        /// Open all interfaces and start to serve clients
        /// </summary>
        public static void Start()
        {
            Logger.Log("Starting NetFluid Engine");
            Logger.Log("Loading calling assembly");

            var assembly = Assembly.GetEntryAssembly();
            var location = assembly.Location;
            
            if(string.IsNullOrEmpty(location))
                Load(assembly);
            else
                Load(location);

            _hosts.ForEach(x =>
            {
                Logger.Log("Starting host:"+x.Key);

                if (x.Value.SSL)
                    listener.Prefixes.Add("https://" + x.Key + "/");
                else
                    listener.Prefixes.Add("http://" + x.Key +"/");
            });

            listener.Prefixes.Add("http://*/");

            Logger.Log("NetFluid web application running");

            listener.Start();

            for (; ;)
            {
                var bcontext = listener.GetContext();
                Task.Factory.StartNew(() =>
                {
                    var cnt = new Context(bcontext);
                    Serve(cnt);
                    //cnt.Close();
                });
            }
        }

        /// <summary>
        /// Load a web application into the virtual host
        /// </summary>
        /// <param name="host">virtual host name</param>
        /// <param name="assemblyPath">physical path to the assembly file</param>
        public static void LoadHost(string host, string assemblyPath)
        {
            LoadHost(host, Assembly.LoadFile(Path.GetFullPath(assemblyPath)));
        }

        /// <summary>
        /// Load all types of assembly under a new virtual host
        /// </summary>
        /// <param name="host">virtual host name</param>
        /// <param name="assembly">assembly to load</param>
        public static void LoadHost(string host, Assembly assembly)
        {
            Logger.Log("Loading " + assembly + " into "+host+" host");

            try
            {
                var pages = assembly.GetTypes();

                foreach (Type p in pages)
                {
                    Logger.Log("Loading " + p.Name);
                    if (p.HasAttribute<VirtualHostAttribute>(true))
                    {
                        foreach (string h in p.CustomAttribute<VirtualHostAttribute>(true).Select(x => x.Name))
                        {
                            if(h.EndsWith(".*"))
                            {
                                foreach (var app in Engine.Hosts)
                                {
                                    Host(h.Substring(0,h.Length-2)+app.Name).Load(p);
                                }
                            }
                            else
                            {
                                Host(h).Load(p);
                            }
                        }
                    }
                    else
                    {
                        Host(host).Load(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error during loading " + assembly + " as " + host + " host", ex);
            }
        }

        /// <summary>
        /// Load a web application into default host
        /// </summary>
        /// <param name="assemblyPath">physical path to the assembly file</param>
        public static void Load(string assemblyPath)
        {
            Load(Assembly.LoadFile(Path.GetFullPath(assemblyPath)));
        }

        /// <summary>
        /// Load all types in assembly as default webapplication
        /// </summary>
        /// <param name="assembly">assembly to be loaded</param>
        public static void Load(Assembly assembly)
        {
            Logger.Log("Loading "+assembly+" into default host");

            try
            {
                var pages = assembly.GetTypes().ToArray();

                foreach (var p in pages)
                {
                    Logger.Log("Loading "+ p.Name);
                    if (p.HasAttribute<VirtualHostAttribute>(true))
                    {
                        foreach (string h in p.CustomAttribute<VirtualHostAttribute>(true).Select(x => x.Name))
                        {
                            if (h.EndsWith(".*"))
                            {
                                foreach (var app in Hosts)
                                {
                                    Host(h.Substring(0, h.Length - 2) + app.Name).Load(p);
                                }
                            }
                            else
                            {
                                Host(h).Load(p);
                            }
                        }
                    }
                    else
                    {
                        DefaultHost.Load(p);
                    }
                    Engine.Logger.Log(p.Name + " loaded");
                }
            }
            catch (ReflectionTypeLoadException lex)
            {
                foreach (var loader in lex.LoaderExceptions)
                {
                    Logger.Log("Error during loading type " + loader.Message + " as default host",loader);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error during loading " + assembly + " as default host", ex);
            }
        }

        public static void Stop()
        {
            listener.Stop();
        }
    }
}