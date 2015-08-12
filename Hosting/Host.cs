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

using Netfluid.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Netfluid
{
    /// <summary>
    /// Virtual host manager
    /// </summary>
    public class Host
    {
        public List<Route> Routes { get; private set; }
        public List<Filter> Filters { get; private set; }
        public List<Trigger> Triggers { get; private set; }
        public List<CallOn> StatusCodeHandlers { get; private set; }

        private readonly List<MethodExposer> _instances;

        public string Name { get; private set; }
        public List<IPublicFolder> PublicFolders { get; set; }
        public ISessionManager Sessions { get; set; }
        public bool SSL { get; set; }
       
        internal Host(string name)
        {
            Name = name;
            Filters = new List<Filter>();
            Triggers = new List<Trigger>();
            Routes = new List<Route>();
            StatusCodeHandlers = new List<CallOn>();
            _instances = new List<MethodExposer>();

            PublicFolders = new List<IPublicFolder>();
            Sessions = new MemorySessionManager();
            SSL = false;
        }

        void loadInstance(Type type)
        {
            if (!_instances.Any(x => x.GetType().Equals(type)))
                try
                {
                    _instances.Add(type.CreateIstance() as MethodExposer);
                }
                catch (Exception ex)
                {
                    Engine.Logger.Log("Failure in " + type + " instancing", ex.InnerException);
                }
        }

        internal void OnServerStart()
        {
            foreach (var type in _instances.Select(x=>x.GetType()))
            {
                var m = type.GetMethod("OnServerStart",BindingFlags.FlattenHierarchy);

                if(m!= null && m.IsOverride())
                {
                    try
                    {
                        var instance = type.CreateIstance() as MethodExposer;
                    }
                    catch (Exception ex)
                    {
                        Engine.Logger.Log("Exception during " + type.Name + ".OnServerStart", ex);
                    }
                }
            }
        }

        void Handle(IEnumerable<Route> routes, Context cnt)
        {
            foreach (var route in routes.Where(x => x.Method == cnt.Request.HttpMethod || x.Method == null))
            {
                if (route.Regex == null)
                {
                    route.Handle(cnt);
                }
                else
                {
                    var m = route.Regex.Match(cnt.Request.Url.LocalPath);

                    if (m.Success)
                    {
                        for (int i = 0; i < route.GroupNames.Length; i++)
                        {
                            var q = new QueryValue(route.GroupNames[i], m.Groups[route.GroupNames[i]].Value);
                            q.Origin = QueryValue.QueryValueOrigin.URL;
                            cnt.Values.Add(q.Name, q);
                        }

                        route.Handle(cnt);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// The current virtual host serve the given context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            if (cnt.Request.HttpMethod.ToLowerInvariant() == "options")
            {
                #region options
                var origin = cnt.Request.Headers["Origin"] ?? "*";
                cnt.Response.Headers.Set("Access-Control-Allow-Origin", origin);

                var headers = cnt.Request.Headers["Access-Control-Request-Headers"] ?? "*";
                cnt.Response.Headers.Set("Access-Control-Allow-Headers", headers);
                cnt.Response.Headers.Set("Access-Control-Max-Age", "360000");
                cnt.Response.Headers.Set("Access-Control-Allow-Methods", "GET, HEAD, POST, TRACE, OPTIONS, PUT, DELETE");
                cnt.Response.Close();
                return;

                #endregion
            }

            Handle(Filters, cnt);

            if (!cnt.IsOpen)
                return;

            Handle(Triggers, cnt);

            Handle(Routes, cnt);

            if (!cnt.IsOpen)
                return;

            for (int i = 0; i < PublicFolders.Count; i++)
            {
                if (PublicFolders[i].TryGetFile(cnt))
                {
                    cnt.Close();
                    return;
                }
            }

            cnt.Response.StatusCode = (int)StatusCode.NotFound;

            Handle(StatusCodeHandlers, cnt);

            cnt.Close();
        }

        public void Load(Type p)
        {
            if (!p.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Loaded types must inherit NetFluid.MethodExposer");

            loadInstance(p);

            var prefixes = p.CustomAttribute<RouteAttribute>(true).Select(x=>x.Url);
            if (prefixes.Count() == 0)
                prefixes = new[] { string.Empty };

            foreach (var m in p.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance))
            {
                foreach (var prefix in prefixes)
                {
                    foreach (var att in m.CustomAttribute<RouteAttribute>())
                    {
                        Routes.Add(new Route
                        {
                            Url = prefix + att.Url,
                            Regex = att.Regex,
                            Index = att.Index,
                            MethodInfo = m 
                        });
                    }

                    foreach (var att in m.CustomAttribute<Netfluid.FilterAttribute>())
                    {
                        Filters.Add(new Filter
                        {
                            Url = prefix + att.Url,
                            Regex = att.Regex,
                            Index = att.Index,
                            MethodInfo = m
                        });
                    }

                    foreach (var att in m.CustomAttribute<Netfluid.TriggerAttribute>())
                    {
                        Triggers.Add(new Trigger
                        {
                            Url = prefix + att.Url,
                            Regex = att.Regex,
                            Index = att.Index,
                            MethodInfo = m
                        });
                    }

                    foreach (var att in m.CustomAttribute<CallOnAttribute>())
                    {
                        foreach (var code in att.StatusCode)
                        {
                            Routes.Add(new Route
                            {
                                Url = prefix + att.Url,
                                Regex = att.Regex,
                                Index = att.Index,
                                MethodInfo = m
                            });
                        }
                    }
                }
            }
        }
    }
}
