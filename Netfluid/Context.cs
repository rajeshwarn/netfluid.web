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
using System.Diagnostics;
using System.Security.Principal;
using System.Text;

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

        public HttpListenerContext HttpListernerContext { get; private set; }

        Stopwatch stopwatch;

        public decimal ElapsedTime
        {
            get { return (decimal)stopwatch.ElapsedTicks / (decimal)Stopwatch.Frequency; }
        }

        public IPrincipal User { get { return HttpListernerContext.User; } }

        public event Action<Context> Closed = x => { };

        public string SessionId { get; private set; }

        public QueryValueCollection Values { get; private set; }

        public HttpFileCollection Files { get; private set; }

        public HttpListenerRequest Request { get { return HttpListernerContext.Request; } }

        public HttpListenerResponse Response { get { return HttpListernerContext.Response; } }

        public bool IsOpen { get; private set; }

        public NetfluidHost Host { get; internal set; }

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
                if (writer == null) writer = new StreamWriter(Response.OutputStream,Encoding.UTF8);
                return writer;
            }
        }

        public Context(HttpListenerContext c)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();

            HttpListernerContext = c;
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

            #region GET PARAMETERS
            if(!string.IsNullOrEmpty(c.Request.Url.Query))
            {
                var parts = (c.Request.Url.Query[0] == '?' ? c.Request.Url.Query.Substring(1) : c.Request.Url.Query).Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string s in parts)
                {
                    string[] val = s.Split(new[] { '=' });
                    int count = val.Length;
                    string k = HttpUtility.UrlDecode(val[0]);

                    switch (count)
                    {
                        case 2:
                            string v = HttpUtility.UrlDecode(val[1]);
                            Values.Add(new QueryValue() { Name = k, Value = v, Origin = QueryValue.QueryValueOrigin.GET });
                            break;
                        case 1:
                            Values.Add(new QueryValue() { Name = k, Value = "true", Origin = QueryValue.QueryValueOrigin.GET });
                            break;
                    }
                }
            }
            #endregion

            #region read request
            if (HttpListernerContext.Request.HttpMethod == "GET" || HttpListernerContext.Request.ContentLength64 <= 0) return;

            if (HttpListernerContext.Request.Headers["Content-Type"].StartsWith("application/x-www-form-urlencoded"))
            {
                //FIXME: Aggiungere MAX-POST-SIZE

                var r = new byte[HttpListernerContext.Request.ContentLength64];
                HttpListernerContext.Request.InputStream.Read(r, 0, r.Length);
                string str = HttpListernerContext.Request.ContentEncoding.GetString(r);

                foreach (string s in str.Split('&'))
                {
                    string[] val = s.Split(new[] { '=' });
                    int count = val.Length;
                    string k = HttpUtility.UrlDecode(val[0]);

                    switch (count)
                    {
                        case 2:
                            string v = HttpUtility.UrlDecode(val[1]);
                            Values.Add(new QueryValue() { Name = k, Value = v, Origin = QueryValue.QueryValueOrigin.POST });
                            break;
                        case 1:
                            Values.Add(new QueryValue() { Name = k, Value = "true", Origin = QueryValue.QueryValueOrigin.POST });
                            break;
                    }
                }
            }
            else if (HttpListernerContext.Request.Headers["Content-Type"].StartsWith("multipart/form-data; boundary="))
            {
                var b = HttpListernerContext.Request.Headers["Content-Type"].Substring("multipart/form-data; boundary=".Length);

                var parser = new MultipartFormDataParser(HttpListernerContext.Request.InputStream, b, HttpListernerContext.Request.ContentEncoding);
                parser.Parameters.ForEach(x => Values.Add(new QueryValue { Name = x.Key, Value = x.Value.Data, Origin = QueryValue.QueryValueOrigin.POST }));
                parser.Files.ForEach(x => Files.Add(x));
            }
            #endregion
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Session(string key, object value)
        {
            Host.Sessions.Set(SessionId, key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Session<T>(string key)
        {
            return (T)Host.Sessions.Get(SessionId, key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public dynamic Session(string key)
        {
            return Host.Sessions.Get(SessionId, key);
        }

        public void Dispose()
        {
            stopwatch = null;
            HttpListernerContext = null;
            reader = null;
            writer = null;
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

                Closed(this);
            }
            catch (Exception)
            {
            }
        }

        public static implicit operator HttpListenerContext (Context cnt)
        {
            return cnt.HttpListernerContext;
        }
    }
}