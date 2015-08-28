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
    public class Engine
    {
        Dictionary<string, Host> hosts;
        HttpListener listener;

        public Engine()
        {
            DefaultHost = new Host("*");
            hosts = new Dictionary<string, Host>();
            hosts.Add("*", DefaultHost);


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

            listener = new HttpListener();
        }



        public IEnumerable<string> Prefixes
        {
            get
            {
                return listener.Prefixes.Select(x=>x.ToString());
            }
        }

        /// <summary>
        /// Main of host of the apllication. Any request not handled by virtual hosts will be handled by this one.
        /// </summary>
        public readonly Host DefaultHost;
        public bool ShowException;

        /// <summary>
        /// Rewritable log manager
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// If true log message and request serving flow are shown on the console
        /// </summary>
        public bool DevMode { get; set; }

        /// <summary>
        /// Check if the program is running as administrator
        /// </summary>
        /// <returns></returns>
        public bool IsAdministrator()
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
        public Host this [string host]
        {
            get
            {
                if (host == "*")
                    return DefaultHost;

                Host h;
                if (hosts.TryGetValue(host, out h))
                    return h;

                h = new Host(host);
                hosts.Add(host, h);

                return h;
            }
        }

        public void Serve(Context cnt)
        {
            if (DevMode)
                Console.WriteLine("Serving " + cnt.Request.Url);

            try
            {
                Host host;
                if (hosts.TryGetValue(cnt.Request.Url.Host, out host))
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

                cnt.Close();
            }
            catch (Exception ex)
            {
                throw ex;  
            }
        }


        /// <summary>
        /// Open all interfaces and start to serve clients
        /// </summary>
        public void Start()
        {
            Logger.Log("Starting NetFluid Engine");
            Logger.Log("Loading calling assembly");

            var assembly = Assembly.GetEntryAssembly();
            var location = assembly.Location;
            
            hosts.ForEach(x =>
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

        public void Stop()
        {
            listener.Stop();
        }
    }
}