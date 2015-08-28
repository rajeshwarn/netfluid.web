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
using System.IO;
using System.Net;
using Netfluid.HTTP;
using System.Runtime.CompilerServices;

namespace Netfluid
{
    /// <summary>
    ///     HTTP Context.
    ///     Contains client request, server response and current variables
    /// </summary>
    public class Context : IDisposable
    {
        private StreamReader reader;
        private StreamWriter writer;

        HttpListenerContext context;

        public string SessionId { get; private set; }

        public QueryValueCollection Values { get; private set; }

        public HttpFileCollection Files { get; private set; }

        public HttpListenerRequest Request { get { return context.Request; } }

        public HttpListenerResponse Response { get { return context.Response; } }

        public bool IsOpen { get; private set; }

        public Host Host { get; internal set; }

        public StreamReader Reader
        {
            get
            {
                if (reader == null) reader = new StreamReader(Request.InputStream);
                return reader;
            }
        }

        public StreamWriter Writer
        {
            get
            {
                if (writer == null) writer = new StreamWriter(Response.OutputStream);
                return writer;
            }
        }

        public Context(HttpListenerContext c)
        {
            context = c;
            Values = new QueryValueCollection();
            Files = new HttpFileCollection();
            IsOpen = true;

            var sess = c.Request.Cookies["session"];

            if (sess != null)
                SessionId = sess.Value;
            else
                SessionId = Security.UID()+Security.UID()+Security.UID();

            Response.ContentType = "text/html";
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            //FIXME: valutare qualcosa di ancora piu sicuro
            sess = new Cookie("session", SessionId, "/");
            sess.Expires = DateTime.Now + TimeSpan.FromDays(1);

            c.Response.Cookies.Add(sess);

            #region read request
            if (context.Request.HttpMethod == "GET" || context.Request.ContentLength64 <= 0) return;

            if (context.Request.Headers["Content-Type"].StartsWith("application/x-www-form-urlencoded"))
            {
                //FIXME: Aggiungere MAX-POST-SIZE

                var r = new byte[context.Request.ContentLength64];
                context.Request.InputStream.Read(r, 0, r.Length);
                string str = context.Request.ContentEncoding.GetString(r);

                foreach (string s in str.Split('&'))
                {
                    string[] val = s.Split(new[] { '=' });
                    int count = val.Length;
                    string k = HttpUtility.UrlDecode(val[0]);

                    switch (count)
                    {
                        case 2:
                            string v = HttpUtility.UrlDecode(val[1]);
                            Values.Add(k, v);
                        break;
                        case 1:
                            Values.Add(k, "true");
                        break;
                    }
                }
            }
            else if (context.Request.Headers["Content-Type"].StartsWith("multipart/form-data; boundary="))
            {
                /*var s = new FileStream(Security.TempFile, FileMode.OpenOrCreate);
                context.Request.InputStream.CopyTo(s);
                s.Flush();
                s.Seek(0, SeekOrigin.Begin);*/

                var b = context.Request.Headers["Content-Type"].Substring("multipart/form-data; boundary=".Length);

                //var parser = new MultipartFormDataParser(s, b, context.Request.ContentEncoding);

                var parser = new MultipartFormDataParser(context.Request.InputStream, b, context.Request.ContentEncoding);
                parser.Parameters.ForEach(x => Values.Add(x.Key, x.Value.Data));
                parser.Files.ForEach(x => Files.Add(x));
            }
            #endregion
        }

        public void Session(string key, object value)
        {
            Host.Sessions.Set(SessionId, key, value);
        }

        public T Session<T>(string key)
        {
            return (T)Host.Sessions.Get(SessionId, key);
        }

        public dynamic Session(string key)
        {
            return Host.Sessions.Get(SessionId, key);
        }

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Close()
        {
            if (!IsOpen) return;

            try
            {
                IsOpen = false;
                Writer.Flush();
                Response.OutputStream.Flush();
                Response.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}