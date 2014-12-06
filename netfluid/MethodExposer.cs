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

using NetFluid.HTTP;

namespace NetFluid
{
    public class MethodExposer
    {
        public MethodExposer()
        {
        }

        public MethodExposer(Context cnt)
        {
            Context = cnt;
        }
        
        /// <summary>
        /// Syntactic sugar for Context.Response
        /// </summary>
        public HttpResponse Response
        {
            get { return Context.Response; }
        }

        /// <summary>
        /// Syntactic sugar for Context.Request
        /// </summary>
        public HttpRequest Request
        {
            get { return Context.Request; }
        }

        /// <summary>
        /// Syntactic sugar for Context.Request.Files
        /// </summary>
        public HttpFileCollection Files
        {
            get { return Context.Request.Files; }
        }

        /// <summary>
        /// Used by the Engine to excute exposed methods
        /// </summary>
        public Context Context { get; set; }

        /// <summary>
        /// Syntactic sugar for Context.Session
        /// </summary>
        public void Session(string name, object obj)
        {
            Context.Session(name, obj);
        }

        /// <summary>
        /// Syntactic sugar for Context.Session
        /// </summary>
        public T Session<T>(string name)
        {
            return Context.Session<T>(name);
        }

        /// <summary>
        /// Syntactic sugar for Context.Session
        /// </summary>
        public dynamic Session(string name)
        {
            return Context.Session(name);
        }

        /// <summary>
        /// Send Response  headers to the client and write byte directly to the client (via OutputStream)
        /// </summary>
        public void SendRaw(byte[] bytes)
        {
            Context.SendHeaders();
            Context.OutputStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// URL encode (http://www.w3schools.com/tags/ref_urlencode.asp)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string URLEncode(string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        /// <summary>
        /// URL decode (http://www.w3schools.com/tags/ref_urlencode.asp)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string URLDecode(string str)
        {
            return HttpUtility.UrlDecode(str);
        }


        public string HTMLEncode(string str)
        {
            return HttpUtility.HtmlEncode(str);
        }

        public string HTMLDecode(string str)
        {
            return HttpUtility.HtmlDecode(str);
        }
    }
}