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
        private class RouteTarget
        {
            public string Url;
            public string Method;
            public Regex Regex;
            public MethodInfo MethodInfo;
            public Type Type;
            public ParameterInfo[] Parameters;
            public string[] GroupNames;
            public int Index;
            public readonly Host Host;

            public RouteTarget(Host host)
            {
                Host = host;
            }

            public virtual void Handle(Context cnt)
            {
                MethodExposer exposer = null;
                object[] args;
                IResponse resp;

                #region ARGS
                try
                {
                    exposer = Type.CreateIstance() as MethodExposer;
                    exposer.Context = cnt;
                    exposer.Host = Host;

                    args = null;

                    if (Parameters != null && Parameters.Length > 0)
                    {
                        args = new object[Parameters.Length];
                        for (int i = 0; i < Parameters.Length; i++)
                        {
                            if (cnt.Values.Contains(Parameters[i].Name))
                            {
                                args[i] = cnt.Values[Parameters[i].Name].Parse(Parameters[i].ParameterType);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError(cnt, ex);
                    return;
                }
                #endregion

                #region RESPONSE
                try
                {
                    resp = MethodInfo.Invoke(exposer, args) as IResponse;

                    if (resp != null)
                        resp.SetHeaders(cnt);
                }
                catch (Exception ex)
                {
                    ShowError(cnt, ex.InnerException);
                    return;
                }
                #endregion

                try
                {
                    if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                        resp.SendResponse(cnt);

                    cnt.Close();
                }
                catch (Exception ex)
                {
                    ShowError(cnt,ex);
                }
            }

            public void ShowError(Context cnt, Exception ex)
            {
                try
                {
                    Engine.Logger.Log("exception serving " + cnt.Request.Url, ex);

                    if (ex is TargetInvocationException)
                        ex = ex.InnerException;
                    
                    if (Engine.ShowException)
                        cnt.Writer.Write(ex);
                }
                catch
                {

                }
                cnt.Close();
            }
        }

        private class Filter : RouteTarget
        {
            public Filter(Host host) :base(host)
            {
            }

            public override void Handle(Context cnt)
            {
                try
                {
                    var exposer = Type.CreateIstance() as MethodExposer;
                    exposer.Context = cnt;
                    exposer.Host = Host;

                    var args = new object[] { null };

                    if ((bool)MethodInfo.Invoke(exposer, args) && args[0] != null)
                    {
                        var resp = args[0] as IResponse;

                        if (resp != null)
                            resp.SetHeaders(cnt);

                        try
                        {
                            if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                                resp.SendResponse(cnt);
                        }
                        catch (Exception ex)
                        {
                            if (Engine.ShowException)
                            {
                                cnt.Writer.Write(ex.ToString());
                            }
                        }
                        cnt.Close();
                    }
                }
                catch (Exception ex)
                {
                    ShowError(cnt, ex.InnerException);
                }
            }
        }

        private class Trigger:RouteTarget
        {
            public Trigger(Host host) :base(host)
            {
            }

            public override void Handle(Context cnt)
            {
                var exposer = Type.CreateIstance() as MethodExposer;
                exposer.Context = cnt;
                exposer.Host = Host;

                object[] args = null;

                if (Parameters != null && Parameters.Length > 0)
                {
                    args = new object[Parameters.Length];
                    for (int i = 0; i < Parameters.Length; i++)
                    {
                        var q = cnt.Values[Parameters[i].Name];
                        if (q != null)
                            args[i] = q.Parse(Parameters[i].ParameterType);
                    }
                }

                try
                {
                    MethodInfo.Invoke(exposer, args);
                }
                catch (Exception ex)
                {
                    Engine.Logger.Log("exception serving "+cnt.Request.Url, ex.InnerException);
                }
            }
        }

        List<RouteTarget> _routes;
        List<Filter> _filters;
        List<Trigger> _triggers;
        Dictionary<StatusCode,RouteTarget> _callOn;

        private readonly List<MethodExposer> _instances;

        public string Name { get; private set; }
        public List<IPublicFolder> PublicFolders { get; set; }
        public ISessionManager Sessions { get; set; }
        public bool SSL { get; set; }
       
        internal Host(string name)
        {
            Name = name;
            _filters = new List<Filter>();
            _triggers = new List<Trigger>();
            _routes = new List<RouteTarget>();
            _callOn = new Dictionary<StatusCode,RouteTarget>();
            _instances = new List<MethodExposer>();

            PublicFolders = new List<IPublicFolder>();
            Sessions = new MemorySessionManager();
            SSL = false;
        }

        static Regex getRegex(string url)
        {
            var urlRegex = url;
            var find = new Regex(":[^//]+");
            foreach (Match item in find.Matches(url))
            {
                urlRegex = urlRegex.Replace(item.Value, "(?<" + item.Value.Substring(1) + ">[^//]+?)");
            }
            return new Regex("^"+urlRegex+"$");
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

        public void AddTrigger(MethodInfo exposedMethod, Regex regex = null, string httpMethod = null, int index = 99999)
        {
            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name + " does not have a parameterless constructor");

            if (!exposedMethod.ReturnType.Implements(typeof(void)))
                throw new TypeLoadException("Exposed method must returns void");

            _triggers.Add(new Trigger(this) { Url = null, Method = httpMethod, MethodInfo = exposedMethod, Regex = regex, Type = type, Index = index });
            _triggers = _triggers.OrderByDescending(x => x.Index).ToList();

            loadInstance(type);
        }

        public void AddTrigger(MethodInfo exposedMethod, string url=null, string httpMethod = null, int index = 99999)
        {
            if (string.IsNullOrWhiteSpace(url)) url = null;

            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name+" does not have a parameterless constructor");

            if (!exposedMethod.ReturnType.Implements(typeof(void)))
                throw new TypeLoadException("Exposed method must returns void");

            var regex = string.IsNullOrEmpty(url) ? null : getRegex(url);

            _triggers.Add(new Trigger(this) { Url = url, Method = httpMethod, MethodInfo = exposedMethod, Regex = regex, Type = type, Index = index });
            _triggers = _triggers.OrderByDescending(x => x.Index).ToList();

            loadInstance(type);
        }

        public void AddFilter(MethodInfo exposedMethod, Regex regex = null, string httpMethod = null, int index = 99999)
        {
            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name + " does not have a parameterless constructor");

            if (!exposedMethod.ReturnType.Implements(typeof(bool)))
                throw new TypeLoadException("Filter methods must returns a bool");

            var args = exposedMethod.GetParameters();
            if (args.Length != 1 || args[0].ParameterType.FullName != "NetFluid.IResponse&")
                throw new TypeLoadException("Filters must have one parameter (ref IResponse)");


            _filters.Add(new Filter(this) { Url = null, Method = httpMethod, MethodInfo = exposedMethod, Regex = regex, Type = type, Index = index });
            _filters = _filters.OrderByDescending(x => x.Index).ToList();

            loadInstance(type);
        }

        public void AddFilter(MethodInfo exposedMethod, string url=null, string httpMethod = null, int index = 99999)
        {
            if (string.IsNullOrWhiteSpace(url)) url = null;

            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name + " does not have a parameterless constructor");

            if (!exposedMethod.ReturnType.Implements(typeof(bool)))
                throw new TypeLoadException("Filter methods must returns a bool");

            var args = exposedMethod.GetParameters();
            if (args.Length != 1 || args[0].ParameterType.FullName != "NetFluid.IResponse&")
                throw new TypeLoadException("Filters must have one parameter (ref IResponse)");

            var regex = url != null ? getRegex(url) : null;

            _filters.Add(new Filter(this) { Url = url, Method = httpMethod, MethodInfo = exposedMethod, Regex = regex, Type = type, Index = index });
            _filters = _filters.OrderByDescending(x => x.Index).ToList();

            loadInstance(type);
        }

        public void AddStatusCodeHandler(StatusCode code, MethodInfo exposedMethod)
        {
            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name + " does not have a parameterless constructor");

            if (!exposedMethod.ReturnType.Implements(typeof(IResponse)))
                throw new TypeLoadException("Exposed methods must returns an IResponse object");

            if (code==StatusCode.AnyError)
                foreach (StatusCode c in Enum.GetValues(typeof(StatusCode)))
                {
                    if (c >= StatusCode.BadRequest && c <= StatusCode.UserAccessDenied)
                        AddStatusCodeHandler(c, exposedMethod);
                }

            if (code == StatusCode.AnyClientError)
                foreach (StatusCode c in Enum.GetValues(typeof(StatusCode)))
                {
                    if (c >= StatusCode.BadRequest && c <= StatusCode.BlockedbyWindowsParentalControls)
                        AddStatusCodeHandler(c, exposedMethod);
                }

            if (code == StatusCode.AnyServerError)
                foreach (StatusCode c in Enum.GetValues(typeof(StatusCode)))
                {
                    if (c >= StatusCode.InternalServerError && c <= StatusCode.UserAccessDenied)
                        AddStatusCodeHandler(c, exposedMethod);
                }


            if(!_callOn.ContainsKey(code))
                _callOn.Add(code, new RouteTarget(this)
                {
                    Type = type,
                    MethodInfo = exposedMethod
                });

            loadInstance(type);
        }


        public void AddRoute(Regex regex, MethodInfo exposedMethod, string httpMethod = null, int index = 99999)
        {
            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name + " does not have a parameterless constructor");

            if (!exposedMethod.ReturnType.Implements(typeof(IResponse)))
                throw new TypeLoadException("Exposed methods must returns an IResponse object");

            _routes.Add(new RouteTarget(this) { Method = httpMethod, MethodInfo = exposedMethod, Regex = regex, Type = type, GroupNames = regex.GetGroupNames(), Parameters = exposedMethod.GetParameters(), Index = index });
            _routes = _routes.OrderByDescending(x => x.Index).ToList();

            loadInstance(type);
        }

        public void AddRoute(string url, MethodInfo exposedMethod, string httpMethod = null, int index=99999)
        {
            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if (!type.HasDefaultConstructor())
                throw new TypeLoadException(type.Name + " does not have a parameterless constructor");

            if(!exposedMethod.ReturnType.Implements(typeof(IResponse)))
                throw new TypeLoadException("Exposed methods must returns an IResponse object");

            var regex = getRegex(url);

            _routes.Add(new RouteTarget(this) { Url=url, Method=httpMethod, MethodInfo=exposedMethod,Regex=regex, Type=type, GroupNames=regex.GetGroupNames(), Parameters=exposedMethod.GetParameters(), Index=index });
            _routes = _routes.OrderByDescending(x => x.Index).ToList();

            loadInstance(type);
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

            #region Filters
            foreach (var filter in _filters.Where(x => x.Method == cnt.Request.HttpMethod || x.Method == null))
            {
                if (filter.Regex == null)
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Url + " - " + "matched " + filter.Url);

                    filter.Handle(cnt);
                }
                else
                {
                    var m = filter.Regex.Match(cnt.Request.Url.LocalPath);

                    if (m.Success)
                    {
                        if (Engine.DevMode)
                            Console.WriteLine(cnt.Request.Url + " - " + "matched " + filter.Url);

                        filter.Handle(cnt);
                    }
                }

                if (!cnt.IsOpen)
                    return;
            }
            #endregion

            #region triggers
            if (!cnt.IsOpen)
                return;

            foreach (var trigger in _triggers.Where(x => x.Method == cnt.Request.HttpMethod || x.Method == null))
            {
                if (trigger.Regex == null)
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Url + " - " + "matched " + trigger.Url);

                    trigger.Handle(cnt);
                }
                else
                {
                    var m = trigger.Regex.Match(cnt.Request.Url.LocalPath);

                    if (m.Success)
                    {
                        if (Engine.DevMode)
                            Console.WriteLine(cnt.Request.Url + " - " + "matched " + trigger.Url);

                        trigger.Handle(cnt);
                    }
                }
                if (!cnt.IsOpen)
                    return;
            }
            #endregion

            if (!cnt.IsOpen)
                return;

            foreach (var route in _routes.Where(x => x.Method == cnt.Request.HttpMethod || x.Method == null))
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

                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Url + " - " + "matched " + route.Url);

                    route.Handle(cnt);
                    return;
                }
            }

            if (!cnt.IsOpen)
                return;

            if (Engine.DevMode)
                Console.WriteLine(cnt.Request.Url + " - " + "Looking for a public folder");

            for (int i = 0; i < PublicFolders.Count; i++)
            {
                if (PublicFolders[i].TryGetFile(cnt))
                {
                    cnt.Close();
                    return;
                }
            }

            cnt.Response.StatusCode = (int)StatusCode.NotFound;

            RouteTarget rt;
            if (_callOn.TryGetValue((StatusCode)cnt.Response.StatusCode, out rt))
            {
                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Url + " - " + "Goes to status " + cnt.Response.StatusCode + " handler");

                rt.Handle(cnt);
                return;
            }

            cnt.Response.StatusCode = (int)StatusCode.NotFound;
            cnt.Close();
        }

        public void Load(Type p)
        {
            if (!p.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Loaded types must inherit NetFluid.MethodExposer");

            loadInstance(p);

            var prefixes = p.CustomAttribute<Route>(true).Select(x=>x.Url);
            if (prefixes.Count() == 0)
                prefixes = new[] { string.Empty };

            foreach (var m in p.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance))
            {
                foreach (var prefix in prefixes)
                {
                    foreach (var att in m.CustomAttribute<Route>())
                    {
                        AddRoute(prefix+att.Url, m, att.Method, att.Index);
                    }
                }

                foreach (var prefix in prefixes)
                {
                    foreach (var att in m.CustomAttribute<Netfluid.Filter>())
                    {
                        if (att.Regex != null)
                            AddFilter(m, new Regex(Regex.Escape(prefix) + att.Regex), att.Method, att.Index);
                        else
                            AddFilter(m, prefix + att.Url, att.Method, att.Index);
                    }
                }

                foreach (var prefix in prefixes)
                {
                    foreach (var att in m.CustomAttribute<Netfluid.Trigger>())
                    {
                        if (att.Regex != null)
                            AddTrigger(m, new Regex(Regex.Escape(prefix) + att.Regex), att.Method, att.Index);
                        else
                            AddTrigger(m, prefix + att.Url, att.Method, att.Index);
                    }
                }

                foreach (var att in m.CustomAttribute<CallOn>())
                {
                    foreach (var code in att.StatusCode)
	                {
                        AddStatusCodeHandler(code, m);
	                }
                }
            }
        }
    }
}
