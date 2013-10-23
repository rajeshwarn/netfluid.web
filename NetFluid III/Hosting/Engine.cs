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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using NetFluid.Sessions;
using NetFluid.Cloud;

namespace NetFluid
{
    public static class Engine
    {
        private static readonly Host MainHost;
        private static readonly Dictionary<string, Host> Hosts;

        static Engine()
        {
            MainHost = new Host();
            Hosts = new Dictionary<string, Host>();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Interfaces = new InterfaceManager();
            Sessions = new MemorySessionManager();
            Cluster = new ClusterManager();
            Logger = new Logger();
        }

        public static ILogger Logger { get; set; }
        public static IWebInterfaceManager Interfaces { get; set; }
        public static ISessionManager Sessions { get; set; }
        public static IClusterManager Cluster { get; set; }

        public static bool RunOnMono
        {
            get { return Type.GetType("Mono.Runtime") != null; }
        }

        public static bool DevMode { get; set; }

        public static string RoutesMap
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var item in Hosts)
                {
                    sb.AppendLine(item.Key + ":");
                    sb.AppendLine(item.Value.RoutesMap);
                }
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
            if (!Hosts.ContainsKey(host))
                Hosts.Add(host, new Host());

            return Hosts[host];
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

                    MainHost.Serve(cnt);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Exception, "Exception serving " + cnt.Request.Host + cnt.Request.Url, ex);
                cnt.Response.StatusCode = StatusCode.BadRequest;
                cnt.Close();
            }
        }

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

        public static void AddPublicFolder(string uri, string path, bool immutable)
        {
        	if (immutable)
        		MainHost.AddImmutablePublicFolder(uri,path);
        	else
        		MainHost.AddPublicFolder(uri,path);
        }
        
        public static void AddPublicFolder(string host, string uri, string path, bool immutable)
        {
        	if (immutable)
        		ResolveHost(host).AddImmutablePublicFolder(uri,path);
        	else
        		ResolveHost(host).AddPublicFolder(uri,path);
        }
        
        public static void Start()
        {
            Logger.Log(LogLevel.Debug, "Starting NetFluid Engine");
            Logger.Log(LogLevel.Debug, "Loading calling assembly");
            Load(Assembly.GetEntryAssembly());
            TemplateCompiler.Preload();
            Interfaces.Start();
        }

        public static void LoadHost(string host, Assembly assembly)
        {
            try
            {
                Type[] types = assembly.GetTypes();
                IEnumerable<Type> pages = types.Where(type => type.Inherit(typeof (FluidPage)));

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

        public static void Load(Assembly assembly)
        {
            try
            {
                Type[] types = assembly.GetTypes();
                IEnumerable<Type> pages = types.Where(type => type.Inherit(typeof (FluidPage)));

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
                        MainHost.Load(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Error during loading " + assembly + " as default host", ex);
            }
        }

        public static void AddFowarding(string host, string remote)
        {
            IPAddress ip;
            int port = 80;

            if (remote.Contains(':'))
            {
                if (!int.TryParse(remote.Substring(remote.LastIndexOf(':') + 1), out port))
                    port = 80;

                remote = remote.Substring(0, remote.LastIndexOf(':'));
            }

            if (!IPAddress.TryParse(remote, out ip))
            {
                IPAddress[] addr =
                    Dns.GetHostAddresses(remote).Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray();

                if (addr.Length == 0)
                    throw new Exception("Host " + remote + " not found");

                ip = addr[0];
            }

            TcpFowarding.SetFowarding(host, new IPEndPoint(ip, port));
        }

        public static void SetRedirect(string destination, params string[] fromhost)
        {
            foreach (string f in fromhost)
            {
                SetSmallController(f, (x) =>
                                          {
                                              x.Response.StatusCode = StatusCode.MovedPermanently;
                                              x.Response.Headers["Location"] = destination;
                                              x.Close();
                                          });
            }
        }

        #region DEFAULT HOST

        public static RouteSetter SetSmallController(Action<Context> act)
        {
            MainHost.SetSmallController(act);
            return new RouteSetter();
        }

        public static RouteSetter SetSmallController(Func<Context, bool> condition, Action<Context> act)
        {
            MainHost.SetSmallController(condition, act);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string url, string methodFullname)
        {
            MainHost.SetRoute(url, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string url, Type type, string method)
        {
            MainHost.SetRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRoute(string url, Type type, MethodInfo method)
        {
            MainHost.SetRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string url, string methodFullname)
        {
            MainHost.SetParameterizedRoute(url, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string url, Type type, string method)
        {
            MainHost.SetParameterizedRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetParameterizedRoute(string url, Type type, MethodInfo method)
        {
            MainHost.SetParameterizedRoute(url, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string rgx, string methodFullname)
        {
            MainHost.SetRegexRoute(rgx, methodFullname);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string rgx, Type type, string method)
        {
            MainHost.SetRegexRoute(rgx, type, method);
            return new RouteSetter();
        }

        public static RouteSetter SetRegexRoute(string rgx, Type type, MethodInfo method)
        {
            MainHost.SetRegexRoute(rgx, type, method);
            return new RouteSetter();
        }

        #endregion

        #region IN-APP HOST

        public static RouteSetter SetSmallController(string host, Action<Context> act)
        {
            ResolveHost(host).SetSmallController(act);
            return new RouteSetter();
        }

        public static RouteSetter SetSmallController(string host, Func<Context, bool> condition, Action<Context> act)
        {
            ResolveHost(host).SetSmallController(condition, act);
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