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
using System.Text;

namespace NetFluid
{
    /// <summary>
    ///  Define methods for http authetications
    /// </summary>
    public class Authentication
    {
        /// <summary>
        /// HTTP Basic authentication.
        /// Return false if the user need to be authenticated, true otherwise
        /// </summary>
        /// <param name="cnt">Current context</param>
        /// <param name="realm">Welcome message for the user</param>
        /// <param name="user">Username given by the client</param>
        /// <param name="pass">Pass given by the client</param>
        public static bool Basic(Context cnt, string realm, out string user, out string pass)
        {
            string auth = cnt.Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                cnt.Response.StatusCode = StatusCode.AuthorizationRequired;
                cnt.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"" + realm + "\"");
                cnt.SendHeaders();

                user = string.Empty;
                pass = string.Empty;

                return false;
            }

            int indez = auth.IndexOf(' ');
            if (indez >= 0 && indez < auth.Length)
                auth = auth.Substring(indez + 1);

            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
            int pos = decoded.IndexOf(':');
            if (pos == -1)
                throw new Exception("Invalid basic authentication header");

            pass = decoded.Substring(pos + 1, decoded.Length - pos - 1);
            user = decoded.Substring(0, pos);

            return true;
        }

        /// <summary>
        /// HTTP Digest authentication.
        /// Return false if the user need to be authenticated, true otherwise
        /// </summary>
        /// <param name="cnt">Current context</param>
        /// <param name="realm">Welcome message for the user</param>
        /// <param name="user">Username given by the client</param>
        /// <param name="pass">Pass given by the client</param>
        public static bool Digest(Context cnt, string realm, out string user, out string pass)
        {
            user = string.Empty;
            pass = string.Empty;

            string auth = cnt.Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                cnt.Response.StatusCode = StatusCode.AuthorizationRequired;
                cnt.Response.Headers.Append("WWW-Authenticate",
                                            string.Format(
                                                "Digest realm=\"{0}\", nonce=\"{1}\", opaque=\"{2}\", stale=false, algorithm=MD5, qop=auth",
                                                realm, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")));
                cnt.SendHeaders();
                return false;
            }

            int indez = auth.IndexOf("username=\"") + "username=\"".Length;
            if (indez >= 0 && indez < auth.Length)
                user = auth.Substring(indez);

            indez = user.IndexOf('"');
            if (indez >= 0)
                user = user.Substring(0, indez);

            return true;
        }
    }
}