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
using System.Text;
using NetFluid.Sessions;
using NetFluid.Cloud;

namespace NetFluid
{
    public static class Engine
    {
        private static readonly Dictionary<string, Host> Hosts;

        static Engine()
        {
            DefaultHost = new Host("default");
            Hosts = new Dictionary<string, Host>();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Interfaces = new InterfaceManager();
            Sessions = new MemorySessionManager();
            Cluster = new ClusterManager();
            Logger = new Logger();
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (
                var ass in
                    Directory.GetFiles("./", "*.dll")
                        .Select(dll => Assembly.LoadFile(Path.GetFullPath(dll)))
                        .Where(ass => ass != null && ass.FullName == args.Name))
                return ass;

            return Directory.GetFiles("./", "*.exe").Select(exe => Assembly.LoadFile(Path.GetFullPath(exe))).FirstOrDefault(ass => ass != null && ass.FullName == args.Name);
        }

        private static readonly Host DefaultHost;
        public static ILogger Logger { get; set; }
        public static IWebInterfaceManager Interfaces { get; set; }
        public static ISessionManager Sessions { get; set; }
        public static IClusterManager Cluster { get; set; }

        public static string[] Hostnames
        {
            get { return Hosts.Keys.ToArray(); }
        }

        public static Host Host(string name)
        {
            Host h;
            Hosts.TryGetValue(name, out h);
            return h;
        }

        public static bool RunOnMono
        {
            get { return Type.GetType("Mono.Runtime") != null; }
        }

        public static bool DevMode { get; set; }

        public static string RoutesMap
        {
            get
            {
                var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<application>");
                sb.AppendLine("<hosts>");
                foreach (var item in Hosts)
                {
                    sb.AppendLine(item.Value.RoutesMap);
                }
                sb.AppendLine("/<hosts>");
                sb.AppendLine("</application>");
                return sb.ToString();
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
                Logger.Log(LogLevel.UnHandled, "Unhandled fatal exception occurred.", e.ExceptionObject as Exception);
            else
                Logger.Log(LogLevel.UnHandled, "Unhandled exception occurred.", e.ExceptionObject as Exception);
        }

        private static Host ResolveHost(string host)
        {
            Host h;
            if (Hosts.TryGetValue(host, out h))
                return h;

            h=new Host(host);
            Hosts.Add(host, h);

            return h;
        }

        internal static void Serve(Context cnt)
        {
            if (DevMode)
                Console.WriteLine("Serving " + cnt.Request.Host + cnt.Request.Url);

            try
            {
                Host host;
                if (Hosts.TryGetValue(cnt.Request.Host, out host))
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Exception, "Exception serving " + cnt.Request.Host + cnt.Request.Url, ex);
                cnt.Response.StatusCode = StatusCode.BadRequest;
                cnt.Close();
            }
        }

        /// <summary>
        /// Load NetFluid configuration from a custom app.config path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool LoadAppConfiguration(string path)
        {
            try
            {
                var s = ConfigurationManager.OpenExeConfiguration(path).GetSection("NetFluidSettings");
                var settings = s as Settings;

                if (settings != null)
                {
                    DevMode = settings.DevMode;
                    Sessions.SessionDuration = settings.SessionDuration;
                    Logger.LogLevel = settings.LogLevel;
                    Logger.LogPath = settings.LogPath;

                    foreach (Interface inter in settings.Interfaces)
                        if (string.IsNullOrEmpty(inter.Certificate))
                            Interfaces.AddInterface(inter.IP, inter.Port);
                        else
                            Interfaces.AddInterface(inter.IP, inter.Port, inter.Certificate);

                    foreach (PublicFolder inter in settings.PublicFolders)
                        AddPublicFolder(inter.UriPath, inter.RealPath, inter.Immutable);

                    return true;
                }
                Logger.Log(LogLevel.Warning, "App configuration doesn't contains a valid NetFluid section");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Failed to load app configuration", ex);
                return false;
            }
        }

        /// <summary>
        /// Load NetFluid configuration from app.config
        /// </summary>
        /// <returns></returns>
        public static bool LoadAppConfiguration()
        {
            try
            {
                var settings = ConfigurationManager.GetSection("NetFluidSettings") as Settings;

                if (settings != null)
                {
                    DevMode = settings.DevMode;
                    Sessions.SessionDuration = settings.SessionDuration;
                    Logger.LogLevel = settings.LogLevel;
                    Logger.LogPath = settings.LogPath;

                    foreach (Interface inter in settings.Interfaces)
                        if (string.IsNullOrEmpty(inter.Certificate))
                            Interfaces.AddInterface(inter.IP, inter.Port);
                        else
                            Interfaces.AddInterface(inter.IP, inter.Port, inter.Certificate);

                    foreach (PublicFolder inter in settings.PublicFolders)
                        AddPublicFolder(inter.UriPath, inter.RealPath, inter.Immutable);

                    return true;
                }
                Logger.Log(LogLevel.Warning, "App configuration doesn't contains a valid NetFluid section");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Failed to load app configuration", ex);
                return false;
            }
        }

        /// <summary>
        /// Add a file-downloadable folder
        /// </summary>
        /// <param name="uri">Sub files and folder will be mapped on this uri</param>
        /// <param name="path">Physical path of mapped folder</param>
        /// <param name="immutable">If true files are memory cached</param>
        public static void AddPublicFolder(string uri, string path, bool immutable=false)
        {
        	if (immutable)
        		DefaultHost.AddImmutablePublicFolder(uri,path);
        	else
        		DefaultHost.AddPublicFolder(uri,path);
        }

        /// <summary>
        /// Add a file-downloadable folder on specified host
        /// </summary>
        /// <param name="host">Virtual host of public folder</param>
        /// <param name="uri">Sub files and folder will be mapped on this uri</param>
        /// <param name="path">Physical path of mapped folder</param>
        /// <param name="immutable">If true files are memory cached</param>
        public static void AddPublicFolder(string host, string uri, string path, bool immutable)
        {
        	if (immutable)
        		ResolveHost(host).AddImmutablePublicFolder(uri,path);
        	else
        		ResolveHost(host).AddPublicFolder(uri,path);
        }
        
        /// <summary>
        /// Open all interfaces and start to serve clients
        /// </summary>
        public static void Start()
        {
            Logger.Log(LogLevel.Debug, "Starting NetFluid Engine");
            Logger.Log(LogLevel.Debug, "Loading calling assembly");
            Load(Assembly.GetEntryAssembly());
            Interfaces.Start();
            Logger.Log(LogLevel.Debug, "NetFluid web application running");
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
                var pages = types.Where(type => type.Implements(typeof (IMethodExposer)));

                foreach (Type p in pages)
                {
                    if (p.HasAttribute<VirtualHost>(true))
                    {
                        foreach (string h in p.CustomAttribute<VirtualHost>(true).Select(x => x.Name))
                        {
                            ResolveHost(h).Load(p);
                        }
                    }
                    else
                    {
                        ResolveHost(host).Load(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Error during loading " + assembly + " as " + host + " host", ex);
            }
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
                var pages = types.Where(type => type.Implements(typeof (IMethodExposer)));

                if (!pages.Any())
                {
                    Logger.Log(LogLevel.Error, "No method exposer found in " + assembly);
                    return;
                }

                foreach (Type p in pages)
                {
                    if (p.HasAttribute<VirtualHost>(true))
                    {
                        foreach (string h in p.CustomAttribute<VirtualHost>(true).Select(x => x.Name))
                        {
                            ResolveHost(h).Load(p);
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

        /// <summary>
        /// For SEO purpose. All client requesting a virtual host in fromhost list recieve a "Moved Permanently" response to destination 
        /// </summary>
        /// <param name="destination">The uri where you want to redirect client</param>
        /// <param name="fromhost">Wich host you want to redirect</param>
        public static void SetRedirect(string destination, params string[] fromhost)
        {
            foreach (string f in fromhost)
            {
                SetController(f, (x) =>
                                          {
                                              x.Response.StatusCode = StatusCode.MovedPermanently;
                                              x.Response.Headers["Location"] = destination;
                                              x.Close();
                                          });
            }
        }

        #region DEFAULT HOST
        
        /// <summary>
        /// Every time the default web application recieve a request an function is invoked. If the function return a value different from null the context execution ends.
        /// </summary>
        /// <param name="act">Action to invoke</param>
        /// <param name="name">Friendly name for action in hosting map</param>
        /// <returns></returns>
        public static RouteSetter SetController(Func<Context,object> act,string name="")
        {
            DefaultHost.SetController(act,name);
            return new RouteSetter();
        }

        /// <summary>
        /// Every time the default web application recieve a request an function is invoked. If the function return a value different from null the context execution ends.
        /// </summary>
        /// <param name="condition">If true the function is invoked</param>
        /// <param name="act">The function to invoke</param>
        /// <param name="name">Friendly name for function in hosting map</param>
        /// <returns></returns>
        public static RouteSetter SetController(Func<Context, bool> condition, Func<Context, object> act, string name = "")
        {
            DefaultHost.SetController(condition, act, name);
            return new RouteSetter();
        }

        public static RouteSetter SetController(Action<Context> act, string name = "")
        {
            DefaultHost.SetController(act, name);
            return new RouteSetter();
        }

        public static RouteSetter SetController(Func<Context, bool> condition, Action<Context> act, string name = "")
        {
            DefaultHost.SetController(condition, act, name);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string url, string methodFullname)
        {
            DefaultHost.SetRoute(url, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string url, Type type, string method)
        {
            DefaultHost.SetRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string url, Type type, MethodInfo method)
        {
            DefaultHost.SetRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string url, string methodFullname)
        {
            DefaultHost.SetParameterizedRoute(url, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string url, Type type, string method)
        {
            DefaultHost.SetParameterizedRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string url, Type type, MethodInfo method)
        {
            DefaultHost.SetParameterizedRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string rgx, string methodFullname)
        {
            DefaultHost.SetRegexRoute(rgx, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string rgx, Type type, string method)
        {
            DefaultHost.SetRegexRoute(rgx, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string rgx, Type type, MethodInfo method)
        {
            DefaultHost.SetRegexRoute(rgx, type, method);
            return new RouteSetter();
        }

        #endregion

        #region IN-APP HOST

        public static RouteSetter SetController(string host, Func<Context,object> act, string name = "")
        {
            ResolveHost(host).SetController(act, name);
            return new RouteSetter();
        }

        public static RouteSetter SetController(string host, Func<Context, bool> condition, Func<Context,object> act, string name = "")
        {
            ResolveHost(host).SetController(condition, act, name);
            return new RouteSetter();
        }

        public static RouteSetter SetController(string host, Action<Context> act, string name="")
        {
            ResolveHost(host).SetController(act,name);
            return new RouteSetter();
        }

        public static RouteSetter SetController(string host, Func<Context, bool> condition, Action<Context> act,string name="")
        {
            ResolveHost(host).SetController(condition, act,name);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string host, string url, string methodFullname)
        {
            ResolveHost(host).SetRoute(url, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string host, string url, Type type, string method)
        {
            ResolveHost(host).SetRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string host, string url, Type type, MethodInfo method)
        {
            ResolveHost(host).SetRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string host, string url, string methodFullname)
        {
            ResolveHost(host).SetParameterizedRoute(url, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string host, string url, Type type, string method)
        {
            ResolveHost(host).SetParameterizedRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string host, string url, Type type, MethodInfo method)
        {
            ResolveHost(host).SetParameterizedRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string host, string rgx, string methodFullname)
        {
            ResolveHost(host).SetRegexRoute(rgx, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string host, string rgx, Type type, string method)
        {
            ResolveHost(host).SetRegexRoute(rgx, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string host, string rgx, Type type, MethodInfo method)
        {
            ResolveHost(host).SetRegexRoute(rgx, type, method);
            return new RouteSetter();
        }

        #endregion
    }
}