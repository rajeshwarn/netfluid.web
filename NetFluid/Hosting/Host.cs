﻿// ********************************************************************************************************
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

namespace NetFluid
{
    internal class Host
    {
        private static readonly char[] urlSeparator;
        private static readonly Dictionary<string, Type> Types;

        private readonly string name;

        private readonly Dictionary<StatusCode, RouteTarget> callOn;
        private readonly List<FluidPage> instances;
        private readonly Dictionary<string, RouteTarget> routes;
        private RouteTarget callOnAnyCode;
        private ParamRouteTarget[] parametrized;
        private RegexRouteTarget[] regex;
        private Controller[] controllers;

        private struct PublicFolder
        {
            public string Path;
            public string Uri;
        }
        
        PublicFolder[] folders;
        readonly Dictionary<string,byte[]> immutableData;
        	
        static Host()
        {
            urlSeparator = new[] {'/'};
            Types = new Dictionary<string, Type>();
        }

        internal Host(string name)
        {

            this.name = name;

            controllers = new Controller[0];

            regex = new RegexRouteTarget[0];
            parametrized = new ParamRouteTarget[0];
            routes = new Dictionary<string, RouteTarget>();

            folders = new Host.PublicFolder[0];
            immutableData = new Dictionary<string, byte[]>();

            callOn = new Dictionary<StatusCode, RouteTarget>();
            
            instances = new List<FluidPage>();
        }

        public string RoutesMap
        {
            get
            {
                var sb = new StringBuilder(string.Format("<Host Name=\"{0}\">",this.name));

                if (controllers.Length>0)
                {
                    sb.Append("<Controllers>");
                    foreach (var item in controllers)
                        sb.Append(string.Format("<Controller Name=\"{0}\" Conditional=\"{1}\" />", item.Name, item.Condition != null));

                    sb.Append("</Controllers>");
                }

                sb.Append("<routes>");

                foreach (var item in regex)
                    sb.Append(string.Format("<RegexRoute Name=\"{0}\" Regex=\"{1}\" PointTo=\"{2}.{3}\" />", item.Name, item.Regex, item.Type.FullName, item.Method.Name));

                foreach (var item in parametrized)
                    sb.Append(string.Format("<ParametrizedRoute Name=\"{0}\" Template=\"{1}\" PointTo=\"{2}.{3}\" />", item.Name, item.Template, item.Type.FullName, item.Method.Name));

                foreach (var item in routes)
                    sb.Append(string.Format("<Route Name=\"{0}\" Template=\"{1}\" PointTo=\"{2}.{3}\" />", item.Value.Name, item.Key, item.Value.Type.FullName, item.Value.Method.Name));


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
            foreach (var item in res as IEnumerable)
                c.Writer.Write(item.ToString());
            c.Close();
        }


        private static void Finalize(Context c, MethodInfo method, object target, params object[] args)
        {
            try
            {
                var res = method.Invoke(target, args);

                SendValue(c,res);

                if (!c.WebSocket)
                    c.Close();
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception, "Error on " + c.Request.Url, ex);
            }
        }

        public void Serve(Context cnt)
        {
            try
            {
                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking controllers");

                #region CONTROLLERS

                foreach (var item in controllers)
                {
                    SendValue(cnt, item.Invoke(cnt));
                    
                    if (!cnt.IsOpen)
                        return;
                }

                #endregion

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking regex routes");

                #region REGEX

                foreach (var rr in regex)
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

                    var page = rr.Type.CreateIstance() as FluidPage;
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
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (groups.Contains(parameters[i].Name))
                        {
                            var q = new QueryValue(parameters[i].Name,m.Groups[parameters[i].Name].Value);
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

                for (var i = 0; i < parametrized.Length; i++)
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

                    var page = parametrized[i].Type.CreateIstance() as FluidPage;
                    page.Context = cnt;

                    var parameters = parametrized[i].Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, parametrized[i].Method, page, null);
                        return;
                    }

                    var argUri = cnt.Request.Url.Substring(parametrized[i].Url.Length).Split(urlSeparator,
                                                                                                  StringSplitOptions.
                                                                                                      RemoveEmptyEntries);
                    var args = new object[parameters.Length];
                    for (var j = 0; j < parameters.Length; j++)
                    {
                        if (j < argUri.Length)
                        {
                            var qv = new QueryValue("",argUri[j]);
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

                    var page = route.Type.CreateIstance() as FluidPage;
                    page.Context = cnt;

                    var parameters = route.Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Finalize(cnt, route.Method, page, null);
                        return;
                    }

                    var args = new object[parameters.Length];
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var q = cnt.Request.Values[parameters[i].Name];
                        if ( q != null )
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
	            if (immutableData.TryGetValue(cnt.Request.Url, out content))
	            {
                    cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
                    cnt.Response.Headers["Cache-Control"] = "max-age=29030400";
                    cnt.Response.Headers["Last-Modified"] = DateTime.MinValue.ToString("r");
                    cnt.Response.Headers["Vary"] = "Accept-Encoding";
                    cnt.SendHeaders();
                    cnt.OutputStream.Write(content, 0, content.Length);
                    cnt.Close();
	                return;
	            }
	            #endregion
	            
	            foreach (var item in folders)
	            {
	                if (cnt.Request.Url.StartsWith(item.Uri))
	                {
	                    var path = System.IO.Path.GetFullPath(item.Path + cnt.Request.Url.Substring(item.Uri.Length).Replace('/', System.IO.Path.DirectorySeparatorChar));

                        if (!path.StartsWith(item.Path))
                        {
                            cnt.Response.StatusCode = StatusCode.BadRequest;
                            cnt.Close();
                            return;
                        }
	
	                    if (System.IO.File.Exists(path))
	                    {
	                        cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
	                        cnt.SendHeaders();
	                        var fs = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);
	                        fs.CopyTo(cnt.OutputStream);
	                        cnt.Close();
	                        return;
	                    }
	                }
	            }
	            
	            #endregion

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Checking status code handlers");

                cnt.Response.StatusCode = StatusCode.NotFound;

                RouteTarget rt;
                if (callOn.TryGetValue(cnt.Response.StatusCode, out rt))
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          rt.Type.FullName + "." + rt.Method.Name);

                    var p = rt.Type.CreateIstance() as FluidPage;
                    p.Context = cnt;

                    Finalize(cnt, rt.Method, p, null);
                    return;
                }
                if (callOnAnyCode != null)
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Calling " +
                                          callOnAnyCode.Type.FullName + "." + callOnAnyCode.Method.Name);

                    var p = callOnAnyCode.Type.CreateIstance() as FluidPage;
                    p.Context = cnt;

                    Finalize(cnt, callOnAnyCode.Method, p, null);
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
                            cnt.Writer.WriteLine("<h2>Data</h2>");
                            cnt.Writer.WriteLine("<table>");
                            foreach (var data in ex.Data.Keys)
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
            Type t = null;
            Types.TryGetValue(type, out t);
            return t;
        }

        public void Load(Type page)
        {
            if (!Types.ContainsKey(page.Name))
                Types.Add(page.Name, page);

            if (page.FullName != null && !Types.ContainsKey(page.FullName))
                Types.Add(page.FullName, page);

            if (!page.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("page must inherit NetFluid.FluidPage");

            try
            {
                instances.Add(page.CreateIstance() as FluidPage);
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
                    if (!callOn.ContainsKey(ma.StatusCode))
                        callOn.Add(ma.StatusCode, new RouteTarget {Type = page, Method = m});

                    if (ma.StatusCode == StatusCode.Any)
                        callOnAnyCode = new RouteTarget {Type = page, Method = m};
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
                if (!callOn.ContainsKey(r.StatusCode))
                {
                    callOn.Add(r.StatusCode, new RouteTarget {Type = page, Method = page.GetMethod("Run")});
                }
                if (r.StatusCode == StatusCode.Any)
                    callOnAnyCode = new RouteTarget {Type = page, Method = page.GetMethod("Run")};
            }
        }

        public void AddPublicFolder(string uriPath, string realPath)
        {
            folders = (folders.Concat(new PublicFolder { Path = System.IO.Path.GetFullPath(realPath), Uri = uriPath })).ToArray();
        }
        
        public void AddImmutablePublicFolder(string uriPath, string realPath)
        {
        	var m = System.IO.Path.GetFullPath(realPath);
            var start = uriPath.EndsWith('/') ? uriPath : uriPath + "/";

            if (System.IO.Directory.Exists(m))
            {
                foreach (var x in System.IO.Directory.GetFiles(m, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    var s = x.Substring(m.Length).Replace(System.IO.Path.DirectorySeparatorChar, '/');

                    if (s[0] == '/')
                        s = s.Substring(1);

                    var fileUri = start + s;
                    if (!immutableData.ContainsKey(fileUri))
                    {
                        immutableData.Add(fileUri,System.IO.File.ReadAllBytes(x));
                    }
                }
            }
        }

        public void SetController(Func<Context,object> act, string name = null)
        {
            var controller = new Controller(act) { Condition = null, Name = name };
            controllers = controllers.Push(controller);
        }

        public void SetController(Func<Context, bool> condition,Func<Context,object> act, string name = null)
        {
            var controller = new Controller(act) { Condition = condition, Name = name };
            controllers = controllers.Push(controller);
        }


        public void SetController(Action<Context> act, string name=null)
        {
            var controller = new Controller(act) {Condition = null, Name=name };
            controllers = controllers.Push(controller);
        }

        public void SetController(Func<Context, bool> condition, Action<Context> act, string name = null)
        {
            var controller = new Controller(act) { Condition = condition, Name = name };
            controllers = controllers.Push(controller);
        }

        public void SetRoute(string url, string methodFullname, string name = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (methodFullname == null)
                throw new NullReferenceException("Null method");

            var t = GetType(methodFullname.Substring(0, methodFullname.LastIndexOf('.')));
            if (t == null)
                throw new TypeLoadException(methodFullname.Substring(0, methodFullname.LastIndexOf('.')) + " not found");

            if (!t.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            instances.Add(t.CreateIstance() as FluidPage);

            SetRoute(url, t, methodFullname.Substring(methodFullname.LastIndexOf('.') + 1),name);
        }

        public void SetRoute(string url, Type type, string method, string name = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            var rt = new RouteTarget {Type = type, Method = type.GetMethod(method), Name=name};

            instances.Add(type.CreateIstance() as FluidPage);

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

            if (!type.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");


            var rt = new RouteTarget {Type = type, Method = method,Name=name};

            instances.Add(type.CreateIstance() as FluidPage);

            if (routes.ContainsKey(url))
                routes[url] = rt;
            else
                routes.Add(url, rt);
        }

        public void SetParameterizedRoute(string url, string methodFullname, string name = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (methodFullname == null)
                throw new NullReferenceException("Null method");

            var t = GetType(methodFullname.Substring(0, methodFullname.LastIndexOf('.')));
            if (t == null)
                throw new TypeLoadException(methodFullname.Substring(0, methodFullname.LastIndexOf('.')) + " not found");

            if (!t.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            instances.Add(t.CreateIstance() as FluidPage);

            SetParameterizedRoute(url, t, methodFullname.Substring(methodFullname.LastIndexOf('.') + 1),name);
        }

        public void SetParameterizedRoute(string url, Type type, string method, string name = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            var rt = new ParamRouteTarget {Type = type, Method = type.GetMethod(method), Url = url , Name=name};

            instances.Add(type.CreateIstance() as FluidPage);

            parametrized = parametrized.Concat(new[] {rt}).OrderByDescending(x => x.Url.Length).ToArray();
        }

        public void SetParameterizedRoute(string url, Type type, MethodInfo method, string name = null)
        {
            if (url == null)
                throw new NullReferenceException("Null url");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            var rt = new ParamRouteTarget {Type = type, Method = method, Url = url, Name=name};

            instances.Add(type.CreateIstance() as FluidPage);

            parametrized = parametrized.Concat(new[] {rt}).OrderByDescending(x => x.Url.Length).ToArray();
        }

        public void SetRegexRoute(string rgx, string methodFullname, string name = null)
        {
            if (rgx == null)
                throw new NullReferenceException("Null regex");

            if (methodFullname == null)
                throw new NullReferenceException("Null method");

            var t = GetType(methodFullname.Substring(0, methodFullname.LastIndexOf('.')));
            if (t == null)
                throw new TypeLoadException(methodFullname.Substring(0, methodFullname.LastIndexOf('.')) + " not found");

            if (!t.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            instances.Add(t.CreateIstance() as FluidPage);

            SetRegexRoute(rgx, t, methodFullname.Substring(methodFullname.LastIndexOf('.') + 1),name);
        }

        public void SetRegexRoute(string rgx, Type type, string method, string name = null)
        {
            if (rgx == null)
                throw new NullReferenceException("Null regex");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            var m = type.GetMethod(method);
            if (m == null)
                throw new TypeLoadException(type.FullName + "." + method + " not found");

            var rt = new RegexRouteTarget {Type = type, Method = m, Regex = new Regex(rgx, RegexOptions.Compiled),Name=name};

            instances.Add(type.CreateIstance() as FluidPage);

            regex = regex.Concat(new[] {rt}).ToArray();
        }

        public void SetRegexRoute(string rgx, Type type, MethodInfo method, string name = null)
        {
            if (rgx == null)
                throw new NullReferenceException("Null regex");

            if (method == null)
                throw new NullReferenceException("Null method");

            if (type == null)
                throw new NullReferenceException("Null type");

            if (!type.Inherit(typeof (FluidPage)))
                throw new TypeLoadException("Routed types must inherit NetFluid.FluidPage");

            var rt = new RegexRouteTarget {Type = type, Method = method, Regex = new Regex(rgx, RegexOptions.Compiled),Name=name};

            instances.Add(type.CreateIstance() as FluidPage);

            regex = regex.Concat(new[] {rt}).ToArray();
        }

        #region Nested type: ParamRouteTarget

        private class ParamRouteTarget
        {
            public string Name;
            public MethodInfo Method;
            public Type Type;
            public string Url;

            public string Template
            {
                get
                {
                    return Url + string.Join("/", Method.GetParameters().Select(x => "{" + x.Name + "}"));
                }
            }
        }

        #endregion

        #region Nested type: RegexRouteTarget

        private class RegexRouteTarget
        {
            public string Name;
            public MethodInfo Method;
            public Regex Regex;
            public Type Type;
        }

        #endregion

        #region Nested type: RouteTarget

        private class RouteTarget
        {
            public string Name;
            public MethodInfo Method;
            public Type Type;
        }

        #endregion

        #region Nested type: SmallControllerChecked

        private class Controller
        {
            private readonly MethodInfo methodInfo;
            private readonly object target;

            public string Name;
            public Func<Context, bool> Condition;

            public Controller(Action<Context> action)
            {
                target = action.Target;
                methodInfo = action.Method;
            }

            public Controller(Func<Context,object> function)
            {
                target = function.Target;
                methodInfo = function.Method;
            }

            public object Invoke(Context c)
            {
                if (Condition != null && !Condition(c))
                    return null;
                
                if (Engine.DevMode)
                    Console.WriteLine(c.Request.Host + ":" + c.Request.Url + " - " + "Calling controller");


                return methodInfo.Invoke(target, new[] {c});
            }
        }

        #endregion
    }
}