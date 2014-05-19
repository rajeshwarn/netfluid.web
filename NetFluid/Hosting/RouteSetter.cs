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
using System.Reflection;

namespace NetFluid
{
    /// <summary>
    /// Sintactic sugar class concatenated route programmatically declarations
    /// </summary>
    public class RouteSetter
    {
        /// <summary>
        /// Prevent Routesetter to be declared
        /// </summary>
        internal RouteSetter()
        {
        }


        /// <summary>
        /// Add a returning value controller to the default webapp.
        /// The controller will be executed for each request received from the client
        /// </summary>
        /// <param name="act">funtion to be executed</param>
        /// <returns>if not null terminates execution of the context</returns>
        public RouteSetter SetController(Func<Context,object> act)
        {
            Engine.SetController(act);
            return this;
        }

        /// <summary>
        /// Add a returning value controller to the default webapp.
        /// The controller will be executed for each request received from the client if condition is true
        /// </summary>
        /// <param name="condition">if true act will be executed</param>
        /// <param name="act">funtion to be executed</param>
        /// <returns>if not null terminates execution of the context</returns>
        public RouteSetter SetController(Func<Context, bool> condition, Func<Context, object> act)
        {
            Engine.SetController(condition, act);
            return this;
        }

        /// <summary>
        /// Add a void controller to the default webapp.
        /// The controller will be executed for each request received from the client
        /// </summary>
        /// <param name="act">funtion to be executed</param>
        public RouteSetter SetController(Action<Context> act)
        {
            Engine.SetController(act);
            return this;
        }

        /// <summary>
        /// Add a void controller to the default webapp.
        /// The controller will be executed for each request received from the client if condition is true
        /// </summary>
        /// <param name="condition">if true act will be executed</param>
        /// <param name="act">funtion to be executed</param>
        public RouteSetter SetController(Func<Context, bool> condition, Action<Context> act)
        {
            Engine.SetController(condition, act);
            return this;
        }

        public RouteSetter SetRoute(string url, Type type, string method)
        {
            Engine.SetRoute(url, type, method);
            return this;
        }

        public RouteSetter SetRoute(string url, Type type, MethodInfo method)
        {
            Engine.SetRoute(url, type, method);
            return this;
        }

        public RouteSetter SetParameterizedRoute(string url, string methodFullname)
        {
            Engine.SetParameterizedRoute(url, methodFullname);
            return this;
        }

        public RouteSetter SetParameterizedRoute(string url, Type type, string method)
        {
            Engine.SetParameterizedRoute(url, type, method);
            return this;
        }

        public RouteSetter SetParameterizedRoute(string url, Type type, MethodInfo method)
        {
            Engine.SetParameterizedRoute(url, type, method);
            return this;
        }

        public RouteSetter SetRegexRoute(string rgx, string methodFullname)
        {
            Engine.SetRegexRoute(rgx, methodFullname);
            return this;
        }

        public RouteSetter SetRegexRoute(string rgx, Type type, string method)
        {
            Engine.SetRegexRoute(rgx, type, method);
            return this;
        }

        public RouteSetter SetRegexRoute(string rgx, Type type, MethodInfo method)
        {
            Engine.SetRegexRoute(rgx, type, method);
            return this;
        }

        public RouteSetter SetController(string host, Action<Context> act)
        {
            Engine.SetController(act);
            return this;
        }

        public RouteSetter SetController(string host, Func<Context, bool> condition, Action<Context> act)
        {
            Engine.SetController(condition, act);
            return this;
        }

        public RouteSetter SetRoute(string host, string url, Type type, string method)
        {
            Engine.SetRoute(url, type, method);
            return this;
        }

        public RouteSetter SetRoute(string host, string url, Type type, MethodInfo method)
        {
            Engine.SetRoute(url, type, method);
            return this;
        }

        public RouteSetter SetParameterizedRoute(string host, string url, string methodFullname)
        {
            Engine.SetParameterizedRoute(url, methodFullname);
            return this;
        }

        public RouteSetter SetParameterizedRoute(string host, string url, Type type, string method)
        {
            Engine.SetParameterizedRoute(url, type, method);
            return this;
        }

        public RouteSetter SetParameterizedRoute(string host, string url, Type type, MethodInfo method)
        {
            Engine.SetParameterizedRoute(url, type, method);
            return this;
        }

        public RouteSetter SetRegexRoute(string host, string rgx, string methodFullname)
        {
            Engine.SetRegexRoute(rgx, methodFullname);
            return this;
        }

        public RouteSetter SetRegexRoute(string host, string rgx, Type type, string method)
        {
            Engine.SetRegexRoute(rgx, type, method);
            return this;
        }

        public RouteSetter SetRegexRoute(string host, string rgx, Type type, MethodInfo method)
        {
            Engine.SetRegexRoute(rgx, type, method);
            return this;
        }
    }
}