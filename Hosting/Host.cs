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
using System.Linq.Expressions;
using System.Reflection;

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
        public List<StatusCodeHandler> StatusCodeHandlers { get; private set; }
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
            StatusCodeHandlers = new List<StatusCodeHandler>();

            PublicFolders = new List<IPublicFolder>();
            Sessions = new MemorySessionManager();
            SSL = false;
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

            foreach (var filter in Filters.Where(x => x.HttpMethod == cnt.Request.HttpMethod || x.HttpMethod == null))
            {
                if(filter.Handle(cnt))
                {
                    cnt.Close();
                    return;
                }
            }

            if (!cnt.IsOpen)
                return;

            foreach (var trigger in Triggers.Where(x => x.HttpMethod == cnt.Request.HttpMethod || x.HttpMethod == null))
            {
                trigger.Handle(cnt);
            }

            foreach (var routes in Routes.Where(x => x.HttpMethod == cnt.Request.HttpMethod || x.HttpMethod == null))
            {
                if (routes.Handle(cnt))
                {
                    cnt.Close();
                    return;
                }
            }

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

            foreach (var handler in StatusCodeHandlers.Where(x => x.HttpMethod == cnt.Request.HttpMethod || x.HttpMethod == null))
            {
                if (handler.Handle(cnt))
                {
                    cnt.Close();
                    return;
                }
            }
            cnt.Close();
        }

        public void Map(object obj)
        {
            Load(obj.GetType(), obj);
        }

        public void Map(Type type)
        {
            Load(type,null);
        }

        void Load(Type type, object instance)
        {
            if (instance == null && type.IsInstantiable() && type.HasDefaultConstructor())
                instance = type.CreateIstance();

            var prefixes = type.CustomAttribute<RouteAttribute>(true).Select(x=>x.Url);
            if (prefixes.Count() == 0)
                prefixes = new[] { string.Empty };

            foreach (var m in type.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.FlattenHierarchy))
            {
                foreach (var prefix in prefixes)
                {
                    foreach (var att in m.CustomAttribute<RouteAttribute>())
                    {
                        Routes.Add(new Route
                        {
                            Url = prefix + att.Url,
                            Index = att.Index,
                            MethodInfo = m,
                            Target = instance
                        });
                    }

                    foreach (var att in m.CustomAttribute<FilterAttribute>())
                    {
                        Filters.Add(new Filter
                        {
                            Url = prefix + att.Url,
                            Index = att.Index,
                            MethodInfo = m,
                            Target = instance
                        });
                    }

                    foreach (var att in m.CustomAttribute<TriggerAttribute>())
                    {
                        Triggers.Add(new Trigger
                        {
                            Url = prefix + att.Url,
                            Index = att.Index,
                            MethodInfo = m,
                            Target = instance
                        });
                    }

                    foreach (var att in m.CustomAttribute<StatusCodeHandlerAttribute>())
                    {
                        foreach (var code in att.StatusCode)
                        {
                            StatusCodeHandlers.Add(new StatusCodeHandler
                            {
                                Url = prefix + att.Url,
                                Index = att.Index,
                                MethodInfo = m,
                                Target = instance
                            });
                        }
                    }
                }
            }
        }
    }
}
