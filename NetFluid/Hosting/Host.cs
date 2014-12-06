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

namespace NetFluid
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

            public void Handle(Context cnt)
            {
                var exposer = Type.CreateIstance() as MethodExposer;
                exposer.Context = cnt;
                object[] args = null;

                if (Parameters.Length > 0)
                {
                    args = new object[Parameters.Length];
                    for (int i = 0; i < Parameters.Length; i++)
                    {
                        args[i] = cnt.Request.Values[Parameters[i].Name].Parse(Parameters[i].ParameterType);
                    }
                }

                var resp = MethodInfo.Invoke(exposer, args) as IResponse;

                if (resp != null)
                    resp.SetHeaders(cnt);

                cnt.SendHeaders();

                try
                {
                    if (resp != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                        resp.SendResponse(cnt);
                }
                catch (Exception)
                {
                }


                cnt.Close();
            }
        }

        private readonly List<MethodExposer> _instances;
        private readonly string _name;
        private List<RouteTarget> _routes;
        private readonly Dictionary<StatusCode,RouteTarget> _callOn;
        public IPublicFolderManager PublicFolders;

        internal Host(string name)
        {
            _name = name;
            _instances = new List<MethodExposer>();
            _routes = new List<RouteTarget>();
            _callOn = new Dictionary<StatusCode,RouteTarget>();
            PublicFolders = new DefaultPublicFolderManager();
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

        public void AddStatusCodeHandler(StatusCode code, MethodInfo exposedMethod)
        {
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


            _callOn.Add(code, new RouteTarget
            {
                Type = exposedMethod.DeclaringType,
                MethodInfo = exposedMethod
            });
        }

        public void AddRoute(string url, MethodInfo exposedMethod, string httpMethod = null, int index=99999)
        {
            var type = exposedMethod.DeclaringType;

            if (!type.Inherit(typeof(MethodExposer)))
                throw new TypeLoadException("Exposing type must inherit NetFluid.MethodExposer");

            if(!exposedMethod.ReturnType.Implements(typeof(IResponse)))
                throw new TypeLoadException("Exposed methods must returns an IResponse object");

            var regex = getRegex(url);

            _routes.Add(new RouteTarget { Url=url, Method=httpMethod, MethodInfo=exposedMethod,Regex=regex, Type=type, GroupNames=regex.GetGroupNames(), Parameters=exposedMethod.GetParameters(), Index=index });
            _routes = _routes.OrderByDescending(x => x.Index).ToList();
        }

        /// <summary>
        /// The current virtual host serve the given context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            try
            {
                if (cnt.Request == null)
                    Console.WriteLine("porco dio");

                if (cnt.Request.HttpMethod == null)
                    Console.WriteLine("porca madonna");

                if (cnt.Request.HttpMethod.ToLowerInvariant() == "options")
                {
                    #region options
                    var origin = cnt.Request.Headers["Origin"] ?? "*";
                    cnt.Response.Headers.Set("Access-Control-Allow-Origin", origin);

                    var headers = cnt.Request.Headers["Access-Control-Request-Headers"] ?? "*";
                    cnt.Response.Headers.Set("Access-Control-Allow-Headers", headers);
                    cnt.Response.Headers.Set("Access-Control-Max-Age", "360000");
                    cnt.Response.Headers.Set("Access-Control-Allow-Methods", "GET, HEAD, POST, TRACE, OPTIONS, PUT, DELETE");
                    cnt.SendHeaders();
                    cnt.Close();
                    return;
                    #endregion
                }

                foreach (var route in _routes.Where(x => x.Method == cnt.Request.HttpMethod || x.Method == null))
                {
                    var m = route.Regex.Match(cnt.Request.Url);

                    if (m.Success)
                    {
                        for (int i = 0; i < route.GroupNames.Length; i++)
                        {
                            var q = new QueryValue(route.GroupNames[i], m.Groups[route.GroupNames[i]].Value);
                            cnt.Request.Values.Add(q.Name, q);
                        }

                        if (Engine.DevMode)
                            Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "matched " + route.Url);

                        route.Handle(cnt);
                        return;
                    }
                }

                if (Engine.DevMode)
                    Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Looking for a public folder");

                /*PublicFolders.Serve(cnt);

                RouteTarget rt;
                if (_callOn.TryGetValue(cnt.Response.StatusCode, out rt))
                {
                    if (Engine.DevMode)
                        Console.WriteLine(cnt.Request.Host + ":" + cnt.Request.Url + " - " + "Goes to status " + cnt.Response.StatusCode + " handler");

                    rt.Handle(cnt);
                    return;
                }*/

                cnt.Response.StatusCode = StatusCode.NotFound;
                cnt.Close();
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;

                Engine.Logger.Log(LogLevel.Exception, "Exception during page execution", ex);

                #region SHOW ERROR PAGE IF IN DEV MODE
                if (Engine.DevMode)
                {
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
                #endregion
            }
            cnt.Close();
        }

        public void Load(Type p)
        {
            foreach (var m in p.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance))
            {
                foreach (var att in m.GetCustomAttributes<Route>())
                {
                    AddRoute(att.Url, m, att.Method, att.Index);
                }
            }
        }
    }
}