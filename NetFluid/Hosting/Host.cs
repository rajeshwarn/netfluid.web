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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetFluid
{
    /// <summary>
    /// Virtual host manager
    /// </summary>
    public class Host
    {

        private static readonly char[] UrlSeparator;
        private static readonly Dictionary<string, Type> Types;
        private readonly Dictionary<StatusCode, RouteTarget> _callOn;
        private readonly List<IMethodExposer> _instances;
        private readonly string _name;
        private readonly Dictionary<string, RouteTarget> routes;
        private RouteTarget _callOnAnyCode;
        private Controller[] _controllers;

        public IPublicFolderManager PublicFolderManager;

        private ParamRouteTarget[] _parametrized;
        private RegexRouteTarget[] _regex;

        static Host()
        {
            UrlSeparator = new[] {'/'};
            Types = new Dictionary<string, Type>();
        }

        internal Host(string name)
        {
            _name = name;

            PublicFolderManager = new PublicFolderManager();

            _controllers = new Controller[0];

            _regex = new RegexRouteTarget[0];
            _parametrized = new ParamRouteTarget[0];
            routes = new Dictionary<string, RouteTarget>();
            _callOn = new Dictionary<StatusCode, RouteTarget>();

            _instances = new List<IMethodExposer>();
        }

        /// <summary>
        /// Routes mapped inside this virtual host
        /// </summary>
        public string RoutesMap
        {
            get
            {
                var sb = new StringBuilder(string.Format("<Host Name=\"{0}\">", _name));

                if (_controllers.Length > 0)
                {
                    sb.Append("<Controllers>");
                    foreach (Controller item in _controllers)
                        sb.Append(string.Format("<Controller Name=\"{0}\" Conditional=\"{1}\" />", item.Name,
                            item.Condition != null));

                    sb.Append("</Controllers>");
                }

                sb.Append("<routes>");

                foreach (RegexRouteTarget item in _regex)
                    sb.Append(string.Format("<RegexRoute Name=\"{0}\" Regex=\"{1}\" PointTo=\"{2}.{3}\" />", item.Name,
                        item.Regex, item.Type.FullName, item.Method.Name));

                foreach (ParamRouteTarget item in _parametrized)
                    sb.Append(string.Format("<ParametrizedRoute Name=\"{0}\" Template=\"{1}\" PointTo=\"{2}.{3}\" />",
                        item.Name, item.Template, item.Type.FullName, item.Method.Name));

                foreach (var item in routes)
                    sb.Append(string.Format("<Route Name=\"{0}\" Template=\"{1}\" PointTo=\"{2}.{3}\" />",
                        item.Value.Name, item.Key, item.Value.Type.FullName, item.Value.Method.Name));


                sb.Append("</routes>");
                sb.Append("</host>");

                return sb.ToString();
            }
        }

        private static void SendValue(Context c, object res)
        {
            if (res is IResponse)
            {
                var resp = res as IResponse;
                resp.SetHeaders(c);
                c.SendHeaders();

                if (c.Request.HttpMethod.ToLowerInvariant() == "head")
                    return;

                resp.SendResponse(c);
                c.Close();
                return;
            }

            if (c.Request.HttpMethod.ToLowerInvariant() == "head")
            {
                c.SendHeaders();
                return;
            }

            if (res is IConvertible)
            {
                c.SendHeaders();
                c.Writer.Write(res.ToString());
                c.Close();
                return;
            }

            if (!(res is IEnumerable)) return;

            c.SendHeaders();
            foreach (object item in res as IEnumerable)
                c.Writer.Write(item.ToString());
            c.Close();
        }

        private static void Finalize(Context c, MethodInfo method, object target, params object[] args)
        {
            object res = null;

            
            try
            {
               res = method.Invoke(target, args);
            }
            catch (Exception ex)
            {
                c.Response.StatusCode = StatusCode.InternalServerError;

                if (Engine.DevMode)
                {
                    try
                    {
                        c.SendHeaders();
                        c.Writer.Write(ex.ToHTML());
                    }
                    catch (Exception)
                    {
                    }
                }

                Engine.Logger.Log(LogLevel.Exception, "Error on " + c.Request.Url, ex);
            }

            SendValue(c, res);
            //if (!c.WebSocket)
            //    c.Close();
        }

        /// <summary>
        /// The current virtual host serve the given context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            try
            {
                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking controllers");

                #region CONTROLLERS

                foreach (var value in _controllers.Select(item => item.Invoke(cnt)))
                {
                    SendValue(cnt, value);

                    if (value != null)
                    {
                        cnt.Close();
                        return;
                    }
                }

                #endregion

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking regex routes");

                #region REGEX

                foreach (var rr in _regex)
                {
                    var m = rr.Regex.Match(cnt.Request.Url);

                    if (!m.Success)
                        continue;

                    if (Engine.DevMode)
                    {
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Matched " + rr.Regex);
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          rr.Type.FullName + "." + rr.Method.Name);
                    }

                    var page = rr.Type.CreateIstance() as IMethodExposer;
                    page.Context = cnt;

                    var parameters = rr.Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, rr.Method, page, null);
                        return;
                    }

                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Parsing arguments");

                    var groups = rr.Regex.GetGroupNames();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (groups.Contains(parameters[i].Name))
                        {
                            var q = new QueryValue(parameters[i].Name, m.Groups[parameters[i].Name].Value);
                            args[i] = q.Parse(parameters[i].ParameterType);
                        }
                        else
                        {
                            args[i] = parameters[i].ParameterType.IsValueType
                                ? Activator.CreateInstance(parameters[i].ParameterType)
                                : null;
                        }
                    }
                    Finalize(cnt, rr.Method, page, args);
                    return;
                }

                #endregion

                
                #region PARAMETRIZED

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking parametrized routes");

                foreach (var t in _parametrized)
                {
                    if (!cnt.Request.Url.StartsWith(t.Url))
                        continue;

                    if (Engine.DevMode)
                    {
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Matched " +
                                          t.Url);
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          t.Type.FullName + "." + t.Method.Name);
                    }

                    var page = t.Type.CreateIstance() as IMethodExposer;
                    page.Context = cnt;

                    var parameters = t.Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, t.Method, page, null);
                        return;
                    }

                    var argUri = cnt.Request.Url.Substring(t.Url.Length).Split(UrlSeparator,StringSplitOptions.RemoveEmptyEntries);
                    var args = new object[parameters.Length];
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        if (j < argUri.Length)
                        {
                            var qv = new QueryValue("", argUri[j]);
                            args[j] = qv.Parse(parameters[j].ParameterType);
                        }
                        else
                        {
                            args[j] = parameters[j].ParameterType.IsValueType
                                ? Activator.CreateInstance(parameters[j].ParameterType)
                                : null;
                        }
                    }

                    Finalize(cnt, t.Method, page, args);
                    return;
                }

                #endregion

                #region FIXED ROUTES

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking routes ");

                RouteTarget route;
                if (routes.TryGetValue(cnt.Request.Url, out route))
                {
                    if (Engine.DevMode)
                    {
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Matched " +cnt.Request.Url);
                    }

                    var page = route.Type.CreateIstance() as IMethodExposer;
                    page.Context = cnt;

                    var parameters = route.Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, route.Method, page, null);
                        return;
                    }

                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var q = cnt.Request.Values[parameters[i].Name];
                        if (q != null)
                            args[i] = q.Parse(parameters[i].ParameterType);
                        else
                            args[i] = parameters[i].MissingValue();
                    }

                    Finalize(cnt, route.Method, page, args);
                    return;
                }

                #endregion


                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Looking for a public folder");

                PublicFolderManager.Serve(cnt);

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking status code handlers");

                cnt.Response.StatusCode = StatusCode.NotFound;

                RouteTarget rt;
                if (_callOn.TryGetValue(cnt.Response.StatusCode, out rt))
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          rt.Type.FullName + "." + rt.Method.Name);

                    var p = rt.Type.CreateIstance() as IMethodExposer;
                    p.Context = cnt;

                    Finalize(cnt, rt.Method, p, null);
                    return;
                }
                if (_callOnAnyCode != null)
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          _callOnAnyCode.Type.FullName + "." + _callOnAnyCode.Method.Name);

                    var p = _callOnAnyCode.Type.CreateIstance() as IMethodExposer;
                    p.Context = cnt;

                    Finalize(cnt, _callOnAnyCode.Method, p, null);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Exception occurred");

                if (ex is TargetInvocationException)
                    ex = ex.InnerException;

                Engine.Logger.Log(LogLevel.Exception, "Exception during page execution", ex);

                if (Engine.DevMode)
                    while (ex != null)
                    {
                        cnt.Response.StatusCode = StatusCode.InternalServerError;
                        cnt.SendHeaders();
                        cnt.Writer.WriteLine("<h1>" + ex.GetType().FullName + "</h1>");
                        if (ex.Data.Count > 0)
                        {
                            cnt.Writer.WriteLine("<h2>Write</h2>");
                            cnt.Writer.WriteLine("<table>");
                            foreach (object data in ex.Data.Keys)
                            {
                                cnt.Writer.WriteLine("<tr><td>" + data + "</td><td>" + ex.Data[data] + "</td></tr>");
                            }
                            cnt.Writer.WriteLine("</table>");
                        }
                        cnt.Writer.WriteLine("<h2>HelpLink</h2>");
                        cnt.Writer.WriteLine(ex.HelpLink);

                        cnt.Writer.WriteLine("<h2>Message</h2>");
                        cnt.Writer.WriteLine(ex.Message);

                        cnt.Writer.WriteLine("<h2>Source</h2>");
                        cnt.Writer.WriteLine(ex.Source);

                        cnt.Writer.WriteLine("<h2>StackTrace</h2>");
                        cnt.Writer.WriteLine(ex.StackTrace);

                        cnt.Writer.WriteLine("<h2>TargetSite</h2>");
                        cnt.Writer.WriteLine(ex.TargetSite);

                        if (ex.InnerException != null)
                        {
                            cnt.Writer.WriteLine("<h2>Inner Exception</h2>");
                            ex = ex.InnerException;
                        }
                        else
                        {
                            break;
                        }
                    }
            }
            cnt.Close();
        }

        private static Type GetType(string type)
        {
            Type t;
            Types.TryGetValue(type, out t);
            return t;
        }

        /// <summary>
        /// Load routes from methods attribute of the method exposer ( type must inherit IMethodExposer)
        /// </summary>
        /// <param name="page"></param>
        public void Load(Type page)
        {
            if (!Types.ContainsKey(page.Name))
                Types.Add(page.Name, page);

            if (!Types.ContainsKey(page.FullName))
                Types.Add(page.FullName, page);

            if (!page.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Loaded type " + page + " must implement NetFluid.IMethodExposer interface");

            try
            {
                var exposer = page.CreateIstance() as IMethodExposer;

                try
                {
                    if (exposer != null) 
                        exposer.OnLoad();
                }
                catch (Exception exception)
                {
                    Engine.Logger.Log(LogLevel.Exception,"Error loading "+page+".OnLoad method throw an exception",exception);
                }

                _instances.Add(exposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Error, "Failed to create instance of " + page.FullName, ex);
            }

            foreach (var m in page.GetMethods())
            {
                foreach (var ma in m.CustomAttribute<Route>())
                    SetRoute(ma.Uri, page, m);

                foreach (var ma in m.CustomAttribute<ParametrizedRoute>())
                    SetParameterizedRoute(ma.Uri, page, m);

                foreach (var ma in m.CustomAttribute<RegexRoute>())
                    SetRegexRoute(ma.Uri, page, m);

                foreach (var ma in m.CustomAttribute<CallOn>())
                {
                    if (!_callOn.ContainsKey(ma.StatusCode))
                        _callOn.Add(ma.StatusCode, new RouteTarget {Type = page, Method = m});

                    if (ma.StatusCode == StatusCode.Any)
                        _callOnAnyCode = new RouteTarget {Type = page, Method = m};
                }
            }

            foreach (var r in page.CustomAttribute<Route>(true))
            {
                foreach (var m in page.GetMethods())
                {
                    foreach (var ma in m.CustomAttribute<Route>())
                        SetRoute(r.Uri + ma.Uri, page, m);

                    foreach (var ma in m.CustomAttribute<ParametrizedRoute>())
                        SetParameterizedRoute(r.Uri + ma.Uri, page, m);

                    foreach (var ma in m.CustomAttribute<RegexRoute>())
                        SetRegexRoute(Regex.Escape(r.Uri) + ma.Uri, page, m);
                }
            }

            foreach (var r in page.CustomAttribute<CallOn>(true))
            {
                if (!_callOn.ContainsKey(r.StatusCode))
                {
                    _callOn.Add(r.StatusCode, new RouteTarget {Type = page, Method = page.GetMethod("Run")});
                }
                if (r.StatusCode == StatusCode.Any)
                    _callOnAnyCode = new RouteTarget {Type = page, Method = page.GetMethod("Run")};
            }
        }

        /// <summary>
        /// Given function will be executed on any request, if returned value is not null the context is closed
        /// </summary>
        /// <param name="act">Function to execute</param>
        /// <param name="friendlyname"></param>
        public void SetController(Func<Context, object> act, string friendlyname = null)
        {
            var controller = new Controller(act) { Condition = null, Name = friendlyname };
            _controllers = _controllers.Push(controller);
        }

        public void SetController(Func<Context, bool> condition, Func<Context, object> act, string friendlyname = null)
        {
            var controller = new Controller(act) {Condition = condition, Name = friendlyname};
            _controllers = _controllers.Push(controller);
        }


        public void SetController(Action<Context> act, string friendlyname = null)
        {
            var controller = new Controller(act) { Condition = null, Name = friendlyname };
            _controllers = _controllers.Push(controller);
        }

        public void SetController(Func<Context, bool> condition, Action<Context> act, string friendlyname = null)
        {
            var controller = new Controller(act) { Condition = condition, Name = friendlyname };
            _controllers = _controllers.Push(controller);
        }

        public void SetRoute(string url, Type type, string method, string friendlyname = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Inherit(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must implement NetFluid.IMethodExposer");

            var rt = new RouteTarget {Type = type, Method = type.GetMethod(method), Name = friendlyname};

            try
            {
                _instances.Add(type.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception,"Failed to instance "+type,ex);
            }


            if (routes.ContainsKey(url))
                routes[url] = rt;
            else
                routes.Add(url, rt);
        }

        public void SetRoute(string url, Type type, MethodInfo method, string name = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must implement NetFluid.IMethodExposer");


            var rt = new RouteTarget {Type = type, Method = method, Name = name};

            try
            {
                _instances.Add(type.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Failed to instance " + type, ex);
            }

            if (routes.ContainsKey(url))
                routes[url] = rt;
            else
                routes.Add(url, rt);
        }

        public void SetParameterizedRoute(string url, string methodFullname, string friendlyname = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (methodFullname == null)
                throw new NullReferenceException("Null method");

            Type t = GetType(methodFullname.Substring(0, methodFullname.LastIndexOf('.')));
            if (t == null)
                throw new TypeLoadException(methodFullname.Substring(0, methodFullname.LastIndexOf('.')) + " not found");

            if (!t.Inherit(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must inherit NetFluid.IMethodExposer");

            _instances.Add(t.CreateIstance() as IMethodExposer);

            SetParameterizedRoute(url, t, methodFullname.Substring(methodFullname.LastIndexOf('.') + 1), friendlyname);
        }

        public void SetParameterizedRoute(string url, Type type, string method, string friendlyname = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must inherit NetFluid.IMethodExposer");

            var rt = new ParamRouteTarget {Type = type, Method = type.GetMethod(method), Url = url, Name = friendlyname};

            try
            {
                _instances.Add(type.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Failed to instance " + type, ex);
            }


            _parametrized = _parametrized.Concat(new[] {rt}).OrderByDescending(x => x.Url.Length).ToArray();
        }

        public void SetParameterizedRoute(string url, Type type, MethodInfo method, string friendlyname = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must inherit NetFluid.IMethodExposer");

            var rt = new ParamRouteTarget {Type = type, Method = method, Url = url, Name = friendlyname};

            try
            {
                _instances.Add(type.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Failed to instance " + type, ex);
            }


            _parametrized = _parametrized.Concat(new[] {rt}).OrderByDescending(x => x.Url.Length).ToArray();
        }

        public void SetRegexRoute(string rgx, string methodFullname, string friendlyname = null)
        {
            if (rgx == null)
                throw new NullReferenceException("Null regex");

            if (methodFullname == null)
                throw new NullReferenceException("Null method");

            Type t = GetType(methodFullname.Substring(0, methodFullname.LastIndexOf('.')));
            if (t == null)
                throw new TypeLoadException(methodFullname.Substring(0, methodFullname.LastIndexOf('.')) + " not found");

            if (!t.Inherit(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must inherit NetFluid.IMethodExposer");

            try
            {
                _instances.Add(t.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Failed to instance " + t, ex);
            }


            SetRegexRoute(rgx, t, methodFullname.Substring(methodFullname.LastIndexOf('.') + 1), friendlyname);
        }

        public void SetRegexRoute(string rgx, Type type, string method, string friendlyname = null)
        {
            if (rgx == null)
                throw new NullReferenceException("Null regex");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must inherit NetFluid.IMethodExposer");

            var m = type.GetMethod(method);
            if (m == null)
                throw new TypeLoadException(type.FullName + "." + method + " not found");

            var rt = new RegexRouteTarget
            {
                Type = type,
                Method = m,
                Regex = new Regex(rgx, RegexOptions.Compiled),
                Name = friendlyname
            };

            try
            {
                _instances.Add(type.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Failed to instance " + type, ex);
            }


            _regex = _regex.Concat(new[] {rt}).ToArray();
        }

        public void SetRegexRoute(string rgx, Type type, MethodInfo method, string friendlyname = null)
        {
            if (rgx == null)
                throw new NullReferenceException("Null regex");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must inherit NetFluid.IMethodExposer");

            var rt = new RegexRouteTarget
            {
                Type = type,
                Method = method,
                Regex = new Regex(rgx, RegexOptions.Compiled),
                Name = friendlyname
            };

            try
            {
                _instances.Add(type.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Failed to instance " + type, ex);
            }


            _regex = _regex.Concat(new[] {rt}).ToArray();
        }

        #region Nested type: ParamRouteTarget

        private class ParamRouteTarget
        {
            public MethodInfo Method;
            public string Name;
            public Type Type;
            public string Url;

            public string Template
            {
                get { return Url + string.Join("/", Method.GetParameters().Select(x => "{" + x.Name + "}")); }
            }
        }

        #endregion

        #region Nested type: RegexRouteTarget

        private class RegexRouteTarget
        {
            public MethodInfo Method;
            public string Name;
            public Regex Regex;
            public Type Type;
        }

        #endregion

        #region Nested type: RouteTarget

        private class RouteTarget
        {
            public MethodInfo Method;
            public string Name;
            public Type Type;
        }

        #endregion

        #region Nested type: SmallControllerChecked

        private class Controller
        {
            private readonly MethodInfo _methodInfo;
            private readonly object _target;

            public Func<Context, bool> Condition;
            public string Name;

            public Controller(Action<Context> action)
            {
                _target = action.Target;
                _methodInfo = action.Method;
            }

            public Controller(Func<Context, object> function)
            {
                _target = function.Target;
                _methodInfo = function.Method;
            }

            public object Invoke(Context c)
            {
                if (Condition != null && !Condition(c))
                    return null;

                if (Engine.DevMode)
                    Console.WriteLine(c.Request.Host + ":" + c.Request.Url + " - " + "Calling controller");


                return _methodInfo.Invoke(_target, new[] {c});
            }
        }

        #endregion
    }
}