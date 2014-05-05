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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using NetFluid.HTTP;

namespace NetFluid
{
    public class Host
    {
        private static readonly char[] UrlSeparator;
        private static readonly Dictionary<string, Type> Types;
        private readonly MemoryCache _eTagCache;

        private readonly Dictionary<StatusCode, RouteTarget> _callOn;
        private readonly Dictionary<string, byte[]> _immutableData;
        private readonly List<IMethodExposer> _instances;
        private readonly string _name;
        private readonly Dictionary<string, RouteTarget> routes;
        private RouteTarget _callOnAnyCode;
        private Controller[] _controllers;

        private PublicFolder[] _folders;
        private ParamRouteTarget[] parametrized;
        private RegexRouteTarget[] regex;

        static Host()
        {
            UrlSeparator = new[] {'/'};
            Types = new Dictionary<string, Type>();
        }

        internal Host(string name)
        {
            this._name = name;

            _controllers = new Controller[0];

            regex = new RegexRouteTarget[0];
            parametrized = new ParamRouteTarget[0];
            routes = new Dictionary<string, RouteTarget>();

            _folders = new PublicFolder[0];
            _immutableData = new Dictionary<string, byte[]>();

            _callOn = new Dictionary<StatusCode, RouteTarget>();

            _instances = new List<IMethodExposer>();

            _eTagCache = MemoryCache.Default;
        }

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

                foreach (RegexRouteTarget item in regex)
                    sb.Append(string.Format("<RegexRoute Name=\"{0}\" Regex=\"{1}\" PointTo=\"{2}.{3}\" />", item.Name,
                        item.Regex, item.Type.FullName, item.Method.Name));

                foreach (ParamRouteTarget item in parametrized)
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
            if (res == null)
                return;

            if (res is IResponse)
            {
                var resp = res as IResponse;
                resp.SendResponse(c);
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
            if (!c.WebSocket)
                c.Close();
        }

        public void Serve(Context cnt)
        {
            try
            {
                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking controllers");

                #region CONTROLLERS

                foreach (Controller item in _controllers)
                {
                    SendValue(cnt, item.Invoke(cnt));

                    if (!cnt.IsOpen)
                        return;
                }

                #endregion

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking regex routes");

                #region REGEX

                foreach (RegexRouteTarget rr in regex)
                {
                    Match m = rr.Regex.Match(cnt.Request.Url);

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

                    string[] groups = rr.Regex.GetGroupNames();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (groups.Contains(parameters[i].Name))
                        {
                            var q = new QueryValue(parameters[i].Name, m.Groups[parameters[i].Name].Value);
                            args[i] = q.Parse(parameters[i]);
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

                for (int i = 0; i < parametrized.Length; i++)
                {
                    if (!cnt.Request.Url.StartsWith(parametrized[i].Url))
                        continue;

                    if (Engine.DevMode)
                    {
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Matched " +
                                          parametrized[i].Url);
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          parametrized[i].Type.FullName + "." + parametrized[i].Method.Name);
                    }

                    var page = parametrized[i].Type.CreateIstance() as IMethodExposer;
                    page.Context = cnt;

                    ParameterInfo[] parameters = parametrized[i].Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, parametrized[i].Method, page, null);
                        return;
                    }

                    string[] argUri = cnt.Request.Url.Substring(parametrized[i].Url.Length).Split(UrlSeparator,
                        StringSplitOptions.
                            RemoveEmptyEntries);
                    var args = new object[parameters.Length];
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        if (j < argUri.Length)
                        {
                            var qv = new QueryValue("", argUri[j]);
                            args[j] = qv.Parse(parameters[j]);
                        }
                        else
                        {
                            args[j] = parameters[j].ParameterType.IsValueType
                                ? Activator.CreateInstance(parameters[j].ParameterType)
                                : null;
                        }
                    }

                    Finalize(cnt, parametrized[i].Method, page, args);
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
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Matched " +
                                          cnt.Request.Url);
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          route.Type.FullName + "." + route.Method.Name);
                    }

                    var page = route.Type.CreateIstance() as IMethodExposer;
                    page.Context = cnt;

                    ParameterInfo[] parameters = route.Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, route.Method, page, null);
                        return;
                    }

                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        QueryValue q = cnt.Request.Values[parameters[i].Name];
                        if (q != null)
                            args[i] = q.Parse(parameters[i]);
                        else
                            args[i] = parameters[i].ParameterType.IsValueType
                                ? Activator.CreateInstance(parameters[i].ParameterType)
                                : null;
                    }

                    Finalize(cnt, route.Method, page, args);
                    return;
                }

                #endregion

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Looking for a public folder");

                #region PUBLIC FILES

                #region IMMUTABLE

                byte[] content;
                if (_immutableData.TryGetValue(cnt.Request.Url, out content))
                {
                    if (cnt.Request.Headers.Contains("If-None-Match"))
                    {
                        cnt.Response.StatusCode = StatusCode.NotModified;
                        return;
                    }

                    cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
                    cnt.Response.Headers["Cache-Control"] = "max-age=29030400";
                    cnt.Response.Headers["Last-Modified"] = DateTime.MinValue.ToString("r");
                    cnt.Response.Headers["Vary"] = "Accept-Encoding";

                    //Fake ETag for immutable files
                    cnt.Response.Headers["ETag"] = Security.UID();
                    cnt.SendHeaders();
                    cnt.OutputStream.Write(content, 0, content.Length);
                    cnt.Close();
                    return;
                }

                #endregion

                foreach (var item in _folders)
                {
                    if (!cnt.Request.Url.StartsWith(item.Uri))
                        continue;

                    string path =
                        Path.GetFullPath(item.Path +
                                         cnt.Request.Url.Substring(item.Uri.Length)
                                             .Replace('/', Path.DirectorySeparatorChar));

                    if (!path.StartsWith(item.Path))
                    {
                        cnt.Response.StatusCode = StatusCode.BadRequest;
                        cnt.Close();
                        return;
                    }

                    if (!File.Exists(path))
                        continue;

                    var etag = _eTagCache.Get(path) as string;
                    if (etag != null)
                    {
                        cnt.Response.Headers["ETag"] = "\"" + etag + "\"";
                        if (cnt.Request.Headers.Contains("If-None-Match") &&
                            cnt.Request.Headers["If-None-Match"].Unquote() == etag)
                        {
                            cnt.Response.StatusCode = StatusCode.NotModified;
                            cnt.Close();
                            return;
                        }
                    }
                    else
                    {
                        _eTagCache.Add(path, Security.SHA1Checksum(path), DateTimeOffset.Now + TimeSpan.FromHours(1));
                    }
                    cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
                    cnt.SendHeaders();
                    var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    fs.CopyTo(cnt.OutputStream);
                    cnt.Close();
                    fs.Close();
                    return;
                }

                #endregion

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
                _instances.Add(page.CreateIstance() as IMethodExposer);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Error, "Failed to create instance of " + page.FullName, ex);
            }

            foreach (MethodInfo m in page.GetMethods())
            {
                foreach (Route ma in m.CustomAttribute<Route>())
                    SetRoute(ma.Uri, page, m);

                foreach (ParametrizedRoute ma in m.CustomAttribute<ParametrizedRoute>())
                    SetParameterizedRoute(ma.Uri, page, m);

                foreach (RegexRoute ma in m.CustomAttribute<RegexRoute>())
                    SetRegexRoute(ma.Uri, page, m);

                foreach (CallOn ma in m.CustomAttribute<CallOn>())
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

        public void AddPublicFolder(string uriPath, string realPath)
        {
            if (!Directory.Exists(realPath))
            {
                Engine.Logger.Log(LogLevel.Error,"Failed to add public folder, directory is missing "+realPath);
                return;
            }

            var f = Path.GetFullPath(realPath);

            if (f.Last() != Path.DirectorySeparatorChar)
                f = f + Path.DirectorySeparatorChar;

            var p = new PublicFolder
            {
                Path = f,
                Uri = uriPath
            };
            _folders = (_folders.Concat(p)).ToArray();
        }

        public void AddImmutablePublicFolder(string uriPath, string realPath)
        {
            var m = Path.GetFullPath(realPath);
            var start = uriPath.EndsWith('/') ? uriPath : uriPath + "/";

            if (!Directory.Exists(m))
            {
                Engine.Logger.Log(LogLevel.Error, "Failed to add public folder, directory is missing " + realPath);
                return;
            }

            foreach (string x in Directory.GetFiles(m, "*.*", SearchOption.AllDirectories))
            {
                string s = x.Substring(m.Length).Replace(Path.DirectorySeparatorChar, '/');

                if (s[0] == '/')
                    s = s.Substring(1);

                string fileUri = start + s;
                if (!_immutableData.ContainsKey(fileUri))
                    _immutableData.Add(fileUri, File.ReadAllBytes(x));
            }
        }

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

        public void SetRoute(string url, string methodFullname, string friendlyname = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (methodFullname == null)
                throw new NullReferenceException("Null method");

            Type t = GetType(methodFullname.Substring(0, methodFullname.LastIndexOf('.')));
            if (t == null)
                throw new TypeLoadException(methodFullname.Substring(0, methodFullname.LastIndexOf('.')) + " not found");

            if (!t.Implements(typeof (IMethodExposer)))
                throw new TypeLoadException("Routed types must implement NetFluid.IMethodExposer interface");

            _instances.Add(t.CreateIstance() as IMethodExposer);

            SetRoute(url, t, methodFullname.Substring(methodFullname.LastIndexOf('.') + 1), friendlyname);
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

            _instances.Add(type.CreateIstance() as IMethodExposer);

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

            _instances.Add(type.CreateIstance() as IMethodExposer);

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

            _instances.Add(type.CreateIstance() as IMethodExposer);

            parametrized = parametrized.Concat(new[] {rt}).OrderByDescending(x => x.Url.Length).ToArray();
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

            _instances.Add(type.CreateIstance() as IMethodExposer);

            parametrized = parametrized.Concat(new[] {rt}).OrderByDescending(x => x.Url.Length).ToArray();
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

            _instances.Add(t.CreateIstance() as IMethodExposer);

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

            MethodInfo m = type.GetMethod(method);
            if (m == null)
                throw new TypeLoadException(type.FullName + "." + method + " not found");

            var rt = new RegexRouteTarget
            {
                Type = type,
                Method = m,
                Regex = new Regex(rgx, RegexOptions.Compiled),
                Name = friendlyname
            };

            _instances.Add(type.CreateIstance() as IMethodExposer);

            regex = regex.Concat(new[] {rt}).ToArray();
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

            _instances.Add(type.CreateIstance() as IMethodExposer);

            regex = regex.Concat(new[] {rt}).ToArray();
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

        private struct PublicFolder
        {
            public string Path;
            public string Uri;
        }
    }
}