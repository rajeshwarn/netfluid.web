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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using NetFluid.Sessions;

namespace NetFluid
{
    /// <summary>
    /// Main class of Netfluid framework
    /// </summary>
    public static class Engine
    {
        private static readonly Dictionary<string, Host> _hosts;

        static Engine()
        {
            DefaultHost = new Host("default");
            _hosts = new Dictionary<string, Host>();

            AppDomain.CurrentDomain.UnhandledException += (s, e) => 
            {
                if (e.IsTerminating)
                    Logger.Log(LogLevel.UnHandled, "Unhandled fatal exception occurred.", e.ExceptionObject as Exception);
                else
                    Logger.Log(LogLevel.UnHandled, "Unhandled exception occurred.", e.ExceptionObject as Exception);
            };
            
            Interfaces = new InterfaceManager();
            Sessions = new MemorySessionManager();
            Logger = new Logger();

            var max = (int)Math.Pow(Environment.ProcessorCount, 5);
            var min = (int)Math.Pow(Environment.ProcessorCount, 4);
            ThreadPool.SetMinThreads(min,max);

        }

        /// <summary>
        /// Main of host of the apllication. Any request not handled by virtual hosts will be handled by this one.
        /// </summary>
        public static readonly Host DefaultHost;

        /// <summary>
        /// Rewritable log manager
        /// </summary>
        public static ILogger Logger { get; set; }

        /// <summary>
        /// Rewritable HTTP/S interface manger
        /// </summary>
        public static IWebInterfaceManager Interfaces { get; set; }

        /// <summary>
        /// Rewritable sessions manager
        /// </summary>
        public static ISessionManager Sessions { get; set; }

        /// <summary>
        /// Currently running virtual hosts managers (reversed proxy excluded)
        /// </summary>
        public static IEnumerable<Host> Hosts
        {
            get { return _hosts.Values; }
        }

        /// <summary>
        /// Currently running virtual host names (reversed proxy excluded)
        /// </summary>
        public static string[] Hostnames
        {
            get { return _hosts.Keys.ToArray(); }
        }

        /// <summary>
        /// If true log message and request serving flow are shown on the console
        /// </summary>
        public static bool DevMode { get; set; }

        /// <summary>
        /// XML summary of virtual host and relative routes
        /// </summary>
        public static string RoutesMap
        {
            get
            {
               /* var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<application>");
                sb.AppendLine("<hosts>");
                foreach (var item in _hosts)
                {
                    sb.AppendLine(item.Value.RoutesMap);
                }
                sb.AppendLine("/<hosts>");
                sb.AppendLine("</application>");
                return sb.ToString();*/
                return null;
            }
        }

        /// <summary>
        /// Return the host manager from the host name (reversed proxy excluded)
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

        internal static void Serve(Context cnt)
        {
            if (DevMode)
                Console.WriteLine("Serving " + cnt.Request.Host + cnt.Request.Url);

            Host host;
            if (_hosts.TryGetValue(cnt.Request.Host, out host))
            {
                if (DevMode)
                    Console.WriteLine(cnt.Request.Host + cnt.Request.Url + " - Using host " + cnt.Request.Host);

                host.Serve(cnt);
            }
            else
            {
                if (DevMode)
                    Console.WriteLine(cnt.Request.Host + cnt.Request.Url + " - Using default web application");

                DefaultHost.Serve(cnt);
            }
        }


        /// <summary>
        /// Open all interfaces and start to serve clients
        /// </summary>
        public static void Start()
        {
            Logger.Log(LogLevel.Debug, "Starting NetFluid Engine");
            Logger.Log(LogLevel.Debug, "Loading calling assembly");

            var assembly = Assembly.GetEntryAssembly();
            var location = assembly.Location;
            
            if(string.IsNullOrEmpty(location))
                Load(assembly);
            else
                Load(location);

            Interfaces.Start();
            Logger.Log(LogLevel.Debug, "NetFluid web application running");
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
            try
            {
                var types = assembly.GetTypes();
                var pages = types.Where(type => type.Inherit(typeof (MethodExposer)));

                foreach (Type p in pages)
                {
                    if (p.HasAttribute<VirtualHost>(true))
                    {
                        foreach (string h in p.CustomAttribute<VirtualHost>(true).Select(x => x.Name))
                        {
                            Host(h).Load(p);
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
                Logger.Log(LogLevel.Error, "Error during loading " + assembly + " as " + host + " host", ex);
            }
        }

        /// <summary>
        /// Load a web application
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
            Logger.Log(LogLevel.Debug,"Loading "+assembly+" into default web application");

            try
            {
                var types = assembly.GetTypes();
                var pages = types.Where(type => type.Inherit(typeof (MethodExposer))).ToArray();

                if (!pages.Any())
                {
                    Logger.Log(LogLevel.Error, "No method exposer found in " + assembly);
                    return;
                }

                foreach (var p in pages)
                {
                    if (p.HasAttribute<VirtualHost>(true))
                    {
                        foreach (string h in p.CustomAttribute<VirtualHost>(true).Select(x => x.Name))
                        {
                            Host(h).Load(p);
                        }
                    }
                    else
                    {
                        DefaultHost.Load(p);
                    }
                }
            }
            catch (ReflectionTypeLoadException lex)
            {
                foreach (var loader in lex.LoaderExceptions)
                {
                    Logger.Log(LogLevel.Error, "Error during loading type " + loader.Message + " as default host",loader);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Error during loading " + assembly + " as default host", ex);
            }
        }
    }
}