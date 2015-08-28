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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace Netfluid
{
    /// <summary>
    /// Virtual host manager
    /// </summary>
    public class Host
    {
        HttpListener listener;

        public string Name { get; private set; }
        public RouteCollection<Route> Routes { get; private set; }
        public RouteCollection<Filter> Filters { get; private set; }
        public RouteCollection<Trigger> Triggers { get; private set; }
        public RouteCollection<StatusCodeHandler> StatusCodeHandlers { get; private set; }
        public List<IPublicFolder> PublicFolders { get; set; }
        public ISessionManager Sessions { get; set; }

        static Host()
        {
            var max = Environment.ProcessorCount * 250;
            var min = Environment.ProcessorCount * 10;
            ThreadPool.SetMinThreads(min, max);
            ThreadPool.SetMaxThreads(max, max);
        }

        public Host(string prefix)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            Name = prefix;
            Filters = new RouteCollection<Filter>();
            Triggers = new RouteCollection<Trigger>();
            Routes = new RouteCollection<Route>();
            StatusCodeHandlers = new RouteCollection<StatusCodeHandler>();

            PublicFolders = new List<IPublicFolder>();
            Sessions = new MemorySessionManager();
        }

        public bool HasRoutes
        {
            get
            {
                return (Filters.Any() || Triggers.Any() || Routes.Any() || StatusCodeHandlers.Any() || PublicFolders.Any());
            }
        }

        #region HTTP LISTENER PROPERTIES
        //
        // Summary:
        //     Gets a value that indicates whether System.Net.HttpListener can be used with
        //     the current operating system.
        //
        // Returns:
        //     true if System.Net.HttpListener is supported; otherwise, false.
        public static bool IsSupported { get { return HttpListener.IsSupported; } }
        //
        // Summary:
        //     Gets or sets the scheme used to authenticate clients.
        //
        // Returns:
        //     A bitwise combination of System.Net.AuthenticationSchemes enumeration values
        //     that indicates how clients are to be authenticated. The default value is System.Net.AuthenticationSchemes.Anonymous.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     This object has been closed.
        public AuthenticationSchemes AuthenticationSchemes { get { return listener.AuthenticationSchemes;  } set { listener.AuthenticationSchemes = value; } }
        //
        // Summary:
        //     Gets or sets the delegate called to determine the protocol used to authenticate
        //     clients.
        //
        // Returns:
        //     An System.Net.AuthenticationSchemeSelector delegate that invokes the method used
        //     to select an authentication protocol. The default value is null.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     This object has been closed.
        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate { get { return listener.AuthenticationSchemeSelectorDelegate; } set { listener.AuthenticationSchemeSelectorDelegate = value; } }
        //
        // Summary:
        //     Gets a default list of Service Provider Names (SPNs) as determined by registered
        //     prefixes.
        //
        // Returns:
        //     A System.Security.Authentication.ExtendedProtection.ServiceNameCollection that
        //     contains a list of SPNs.
        public ServiceNameCollection DefaultServiceNames { get { return listener.DefaultServiceNames; } }
        //
        // Summary:
        //     Get or set the System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy
        //     to use for extended protection for a session.
        //
        // Returns:
        //     A System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy
        //     that specifies the policy to use for extended protection.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionPolicy
        //     property, but the System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy.CustomChannelBinding
        //     property was not null.
        //
        //   T:System.ArgumentNullException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionPolicy
        //     property to null.
        //
        //   T:System.InvalidOperationException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionPolicy
        //     property after the System.Net.HttpListener.Start method was already called.
        //
        //   T:System.ObjectDisposedException:
        //     This object is closed.
        //
        //   T:System.PlatformNotSupportedException:
        //     The System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy.PolicyEnforcement
        //     property was set to System.Security.Authentication.ExtendedProtection.PolicyEnforcement.Always
        //     on a platform that does not support extended protection.
        public ExtendedProtectionPolicy ExtendedProtectionPolicy { get { return listener.ExtendedProtectionPolicy; } set { listener.ExtendedProtectionPolicy = value; } }
        //
        // Summary:
        //     Get or set the delegate called to determine the System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy
        //     to use for each request.
        //
        // Returns:
        //     A System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy
        //     that specifies the policy to use for extended protection.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionSelectorDelegate
        //     property, but the System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy.CustomChannelBinding
        //     property must be null.
        //
        //   T:System.ArgumentNullException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionSelectorDelegate
        //     property to null.
        //
        //   T:System.InvalidOperationException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionSelectorDelegate
        //     property after the System.Net.HttpListener.Start method was already called.
        //
        //   T:System.ObjectDisposedException:
        //     This object is closed.
        //
        //   T:System.PlatformNotSupportedException:
        //     An attempt was made to set the System.Net.HttpListener.ExtendedProtectionSelectorDelegate
        //     property on a platform that does not support extended protection.
        public HttpListener.ExtendedProtectionSelector ExtendedProtectionSelectorDelegate { get { return listener.ExtendedProtectionSelectorDelegate; } set { listener.ExtendedProtectionSelectorDelegate = value; } }
        //
        // Summary:
        //     Gets or sets a System.Boolean value that specifies whether your application receives
        //     exceptions that occur when an System.Net.HttpListener sends the response to the
        //     client.
        //
        // Returns:
        //     true if this System.Net.HttpListener should not return exceptions that occur
        //     when sending the response to the client; otherwise false. The default value is
        //     false.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     This object has been closed.
        public bool IgnoreWriteExceptions { get { return listener.IgnoreWriteExceptions; } set { listener.IgnoreWriteExceptions = value; } }
        //
        // Summary:
        //     Gets a value that indicates whether System.Net.HttpListener has been started.
        //
        // Returns:
        //     true if the System.Net.HttpListener was started; otherwise, false.
        public bool IsListening { get { return listener.IsListening; } }
        //
        // Summary:
        //     Gets the Uniform Resource Identifier (URI) prefixes handled by this System.Net.HttpListener
        //     object.
        //
        // Returns:
        //     An System.Net.HttpListenerPrefixCollection that contains the URI prefixes that
        //     this System.Net.HttpListener object is configured to handle.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     This object has been closed.
        public HttpListenerPrefixCollection Prefixes { get { return listener.Prefixes; } }
        //
        // Summary:
        //     Gets or sets the realm, or resource partition, associated with this System.Net.HttpListener
        //     object.
        //
        // Returns:
        //     A System.String value that contains the name of the realm associated with the
        //     System.Net.HttpListener object.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     This object has been closed.
        public string Realm { get { return listener.Realm; } set { listener.Realm = value; } }
        //
        // Summary:
        //     The timeout manager for this System.Net.HttpListener instance.
        //
        // Returns:
        //     Returns System.Net.HttpListenerTimeoutManager.The timeout manager for this System.Net.HttpListener
        //     instance.
        public HttpListenerTimeoutManager TimeoutManager { get { return listener.TimeoutManager; } }
        //
        // Summary:
        //     Gets or sets a System.Boolean value that controls whether, when NTLM is used,
        //     additional requests using the same Transmission Control Protocol (TCP) connection
        //     are required to authenticate.
        //
        // Returns:
        //     true if the System.Security.Principal.IIdentity of the first request will be
        //     used for subsequent requests on the same connection; otherwise, false. The default
        //     value is false.
        //
        // Exceptions:
        //   T:System.ObjectDisposedException:
        //     This object has been closed.
        public bool UnsafeConnectionNtlmAuthentication { get { return listener.UnsafeConnectionNtlmAuthentication; } set { listener.IgnoreWriuteExceptions = value; } }
        #endregion

        #region NETFLUID METHODS
        /// <summary>
        /// The current virtual host serve the given context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            cnt.Host = this;

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
                var value = filter.Handle(cnt);

                if (value == null) return;

                if (value is IResponse)
                {
                    value.SetHeaders(cnt);

                    if (value != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                        value.SendResponse(cnt);
                }
                else if (value is bool)
                {
                    if(value) return;
                }
                else if(value is Stream)
                {
                    value.CopyTo(cnt.Response.OutputStream);
                    return;
                }
                else
                {
                    cnt.Writer.Write(value.ToString());
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
                var value = routes.Handle(cnt);

                if(value == null) return;

                if (value is IResponse)
                {
                    value.SetHeaders(cnt);

                    if (value != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                        value.SendResponse(cnt);
                }
                else if (value is bool)
                {
                    if (value) return;
                }
                else if (value is Stream)
                {
                    value.CopyTo(cnt.Response.OutputStream);
                    return;
                }
                else
                {
                    cnt.Writer.Write(value.ToString());
                    return;
                }
            }

            if (!cnt.IsOpen)
                return;

            for (int i = 0; i < PublicFolders.Count; i++)
            {
                if (PublicFolders[i].TryGetFile(cnt))
                {
                    return;
                }
            }

            cnt.Response.StatusCode = (int)StatusCode.NotFound;

            foreach (var handler in StatusCodeHandlers.Where(x => x.HttpMethod == cnt.Request.HttpMethod || x.HttpMethod == null))
            {
                var value = handler.Handle(cnt);
                if (value is IResponse)
                {
                    value.SetHeaders(cnt);

                    if (value != null && cnt.Request.HttpMethod.ToLowerInvariant() != "head")
                        value.SendResponse(cnt);
                }
                else if (value is bool)
                {
                    if (value) return;
                }
                else if (value is Stream)
                {
                    value.CopyTo(cnt.Response.OutputStream);
                    return;
                }
                else
                {
                    cnt.Writer.Write(value.ToString());
                    return;
                }
            }
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

            foreach (var m in type.GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static))
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

        #endregion
    }
}
