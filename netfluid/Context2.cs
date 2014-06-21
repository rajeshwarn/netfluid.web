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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NetFluid.HTTP;
using NetFluid.IO;

namespace NetFluid
{
    /// <summary>
    ///     HTTP Context.
    ///     Contains client request, server response and current variables
    /// </summary>
    public class Context
    {
        private static readonly char[] Separators = { ' ' };
        private static readonly Version Latest = new Version(1,2);

        private readonly Stopwatch st;

        /// <summary>
        /// Use to measure current Engine performance.If setted is invoked on every action, telling wich uri was served and its relative speed (rps)
        /// </summary>
        public static event Action<long, string, string> Profiling; 


        private readonly MemoryStream _ms;

        /// <summary>
        ///     True if Response.Headers are already sent to the client
        /// </summary>
        public bool HeadersSent;

        /// <summary>
        ///     Read data to the client
        /// </summary>
        public Stream InputStream;

        /// <summary>
        ///     Send data to the client
        /// </summary>
        public Stream OutputStream;

        /// <summary>
        ///     Contains client request variables
        /// </summary>
        public HttpRequest Request;

        /// <summary>
        ///     Contains server response variables
        /// </summary>
        public HttpResponse Response;

        /// <summary>
        ///     Client socket
        /// </summary>
        public Socket Socket;


        /// <summary>
        ///     True if the client is a websocket
        /// </summary>
        public bool WebSocket;

        private StreamReader reader;
        private StreamWriter writer;

        /// <summary>
        ///     Initialize HTTP context from client socket, reading headers and post data
        /// </summary>
        /// <param name="sock">Client socket</param>
        public Context(Socket sock)
        {
            sock.SendTimeout = 150;
            sock.ReceiveTimeout = 150;

            IsOpen = true;
            Secure = false;

            if (Profiling!=null)
            {
                st = new Stopwatch();
                st.Start();
            }

            Socket = sock;

            Request = new HttpRequest();
            Response = new HttpResponse();

            var stream = new NetworkStream(sock);
            OutputStream = stream;
            InputStream = stream;
        }

        /// <summary>
        ///     Initialize HTTPS context from client socket, reading headers and post data
        /// </summary>
        /// <param name="sock">Client socket</param>
        /// <param name="certificate">PFX Certificate</param>
        public Context(Socket sock, X509Certificate2 certificate)
        {
            if (Profiling!=null)
            {
                st = new Stopwatch();
                st.Start();
            }


            Socket = sock;
            Secure = true;

            _ms = new MemoryStream();

            Request = new HttpRequest();
            Response = new HttpResponse();

            var stream = new SslStream(new NetworkStream(sock), false);


            stream.BeginAuthenticateAsServer(certificate, x =>
            {
                try
                {
                    stream.EndAuthenticateAsServer(x);
                    OutputStream = stream;
                    InputStream = stream;

                    Response.StatusCode = StatusCode.BadRequest;
                }
                catch
                {
                    Close();
                }
            }, null);

        }

        /// <summary>
        /// Foward this context to other host and port (<ip/host>:port) default port:80
        /// </summary>
        /// <param name="remote"></param>
        public void FowardTo(string remote)
        {
            Engine.Cluster.Foward(this,remote);
        }

        /// <summary>
        ///     Current session identification Guid
        /// </summary>
        public string SessionId  { get; private set; }

        /// <summary>
        /// True if the context has been created with HTTPS
        /// </summary>
        public bool Secure { get; private set; }

        /// <summary>
        /// True if the context is not even served
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        ///     True if the current HTTP response code is about an error
        /// </summary>
        public bool HaveError
        {
            get { return (Response.StatusCode >= (StatusCode) 400); }
        }

        /// <summary>
        ///     Read string from the client
        /// </summary>
        public StreamReader Reader
        {
            get { return reader = reader ?? new StreamReader(InputStream, Request.ContentEncoding); }
        }

        /// <summary>
        ///     Write string to the client
        /// </summary>
        public StreamWriter Writer
        {
            get { return writer = writer ?? new StreamWriter(OutputStream, Response.ContentEncoding, 1024); }
            internal set { writer = value; }
        }

        /// <summary>
        ///     IP and port on wich the client connection was recieved
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get { return Socket.LocalEndPoint as IPEndPoint; }
        }

        /// <summary>
        ///     IP and port on wich the client connection was started
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get { return Socket.RemoteEndPoint as IPEndPoint; }
        }

        /// <summary>
        ///     True if the connection came from the same machine of the server
        /// </summary>
        public bool IsLocal
        {
            get { return IPAddress.IsLoopback(RemoteEndPoint.Address); }
        }

        string ReadHeader()
        {
            var l = new List<byte>(64);
            reader:
            var b = (byte)InputStream.ReadByte();

            if (b == 10)
                return Encoding.UTF8.GetString(l.ToArray());
            if(b!=13)
                l.Add(b);
            goto reader;
        }

        /// <summary>
        ///     Read request headers
        /// </summary>
        public void ReadHeaders()
        {
            var lines = new List<string>(15);
            
            string line;
            do
            {
                line = ReadHeader();
                if(line!="")
                    lines.Add(line);
            } while (line!="");

            #region FIRST LINE

            var parts = lines[0].Split(Separators, 3);

            if (parts.Length < 3)
            {
                Response.StatusCode = StatusCode.BadRequest;
                return;
            }
            Request.HttpMethod = parts[0];
            Request.RawUrl = parts[1];

            try
            {
                switch (parts[2][7])
                {
                    case '0':
                        Response.ProtocolVersion = HttpVersion.Version10;
                        break;
                    case '1':
                        Response.ProtocolVersion = HttpVersion.Version11;
                        break;
                    case '2':
                        Response.ProtocolVersion = Latest;
                        break;
                    default:
                        Response.ProtocolVersion =
                            Request.ProtocolVersion = Version.Parse(parts[2].Substring("HTTP/".Length));
                        break;
                }
            }
            catch (Exception)
            {
                Response.ProtocolVersion = new Version(0, 0);
            }

            Request.Get = new QueryValueCollection();
            int index = parts[1].IndexOf('?');

            if (index < 0)
            {
                Request.Url = HttpUtility.UrlDecode(Request.RawUrl);
            }
            else
            {
                Request.Url = HttpUtility.UrlDecode(parts[1].Substring(0, index));
                parts[1] = parts[1].Substring(index + 1);

                parts[1].Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ForEach(kv =>
                {
                    int pos = kv.IndexOf('=');

                    var val = pos < 0 ? "true" : HttpUtility.UrlDecode(kv.Substring(pos + 1));
                    var key = pos < 0 ? HttpUtility.UrlDecode(kv) : HttpUtility.UrlDecode(kv.Substring(0, pos));

                    Request.Get.Add(key, val);
                });
            }


            #endregion

            for (int i = 1; i < lines.Count; i++)
            {
                #region GENERIC HEADER

                int colon = lines[i].IndexOf(':');
                if (colon <= 0)
                {
                    Response.StatusCode = StatusCode.BadRequest;
                    return;
                }

                string name = lines[i].Substring(0, colon).Trim();
                string val = lines[i].Substring(colon + 1).Trim();

                Request.Headers.Append(name, val);

                switch (name.ToLowerInvariant())
                {
                    case "accept-language":
                        Request.UserLanguages = val.Split(',');
                        break;
                    case "accept":
                        Request.AcceptTypes = val.Split(',');
                        break;
                    case "content-length":
                        try
                        {
                            Request.ContentLength = Int64.Parse(val.Trim());
                            if (Request.ContentLength < 0)
                                Response.StatusCode = StatusCode.BadRequest;
                        }
                        catch
                        {
                            Response.StatusCode = StatusCode.BadRequest;
                        }
                        break;
                    case "referer":
                        Request.UrlReferrer = val;
                        break;
                    case "cookie":

                        #region COOKIE PARSING

                        string[] cookieStrings = val.Split(new[] {',', ';'});
                        Cookie current = null;
                        int protocolVersion = 0;
                        foreach (string cookieString in cookieStrings)
                        {
                            try
                            {
                                string str = cookieString.Trim();
                                if (str.Length == 0)
                                    continue;

                                int iu = str.IndexOf('=');
                                string prop = iu >= 0 ? str.Substring(0, iu) : "";
                                string value = iu >= 0 ? str.Substring(iu + 1).Trim() : "";

                                #region SWITCH PROPERTIES

                                switch (prop)
                                {
                                    case "$ProtocolVersion":
                                        protocolVersion = Int32.Parse(value.Unquote());
                                        break;
                                    case "$Path":
                                        if (current != null)
                                            current.Path = value;
                                        break;
                                    case "$Domain":
                                        if (current != null)
                                            current.Domain = value;
                                        break;
                                    case "$Port":
                                        if (current != null)
                                            current.Port = value;
                                        break;
                                    default:
                                        if (current != null)
                                            Request.Cookies.Add(current);

                                        current = new Cookie();
                                        int idx = str.IndexOf('=');
                                        if (idx > 0)
                                        {
                                            current.Name = str.Substring(0, idx).Trim();
                                            current.Value = str.Substring(idx + 1).Trim();
                                        }
                                        else
                                        {
                                            current.Name = str.Trim();
                                            current.Value = String.Empty;
                                        }
                                        current.Version = protocolVersion;
                                        break;
                                }

                                #endregion
                            }
                            catch
                            {
                            }
                        }
                        if (current != null)
                            Request.Cookies.Add(current);
                        break;

                        #endregion
                }

                #endregion
            }

            #region WEBSOCKET

            if (Request.Headers.Contains("Sec-WebSocket-Key"))
            {
                string acc = "Sec-WebSocket-Accept: " +
                             Security.SecWebSocketAccept(Request.Headers["Sec-WebSocket-Key"]) + "\r\n";

                if (Response.Headers["Sec-WebSocket-Protocol"] != "")
                    acc += "Sec-WebSocket-Protocol: " + Request.Headers["Sec-WebSocket-Protocol"] + "\r\n";

                acc += "\r\n";

                string h =
                    string.Format(
                        "HTTP/1.1 101 Switching Protocols\r\nConnection:Upgrade\r\nUpgrade:websocket\r\nServer: NetFluid III\r\n" +
                        acc);
                byte[] b = Response.ContentEncoding.GetBytes(h);
                OutputStream.Write(b, 0, b.Length);
                OutputStream.Flush();

                WebSocket = true;
                HeadersSent = true;
                OutputStream = new WebSocketStream(OutputStream);
                InputStream = new WebSocketStream(InputStream);
                writer = new StreamWriter(OutputStream);
                reader = new StreamReader(InputStream);
            }

            #endregion


            if (Request.Cookies["SESSION-ID"] != null)
                SessionId = Request.Cookies["SESSION-ID"].Value;

        }

        public void ReadRequest()
        {
            if (Request.HttpMethod == "GET" || Request.ContentLength <= 0) return;

            var s = new FileStream(Security.TempFile, FileMode.OpenOrCreate);
            InputStream.CopyTo(s, Request.ContentLength);
            s.Flush();
            s.Seek(0, SeekOrigin.Begin);

            if (Request.Headers["Content-Type"].StartsWith("application/x-www-form-urlencoded"))
                PostManager.DecodeUrl(this, s);
            else if (Request.Headers["Content-Type"].StartsWith("multipart/form-data; boundary="))
                PostManager.DecodeMultiPart(this, s);
            else
                InputStream = s;
        }

        /// <summary>
        ///     Send Response headers to the client
        /// </summary>
        public void SendHeaders()
        {
            if (HeadersSent)
                return;

            if (Response.ContentEncoding == null)
                Response.ContentEncoding = Encoding.UTF8;

            if (Response.ContentType != null)
                if (Response.ContentType.IndexOf("charset=", StringComparison.Ordinal) == -1)
                    Response.Headers.Set("Content-Type",
                        Response.ContentType + "; charset=" + Response.ContentEncoding.WebName);
                else
                    Response.Headers.Set("Content-Type", Response.ContentType);


            // They sent both KeepAlive: true and Connection: close!?
            if (!Response.KeepAlive || Response.StatusCode >= (StatusCode)400)
            {
                Response.Headers.Set("Connection", "close");
            }
            else
            {
                Response.Headers.Set("Keep-Alive", "timeout=15,max=100");
                if (Request.ProtocolVersion <= HttpVersion.Version10)
                    Response.Headers.Set("Connection", "keep-alive");
            }

            if (SessionId != null)
            {
                if (Response.Cookies == null)
                    Response.Cookies = new CookieCollection();

                var domainParts = Request.Host.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (domainParts.Length >= 2)
                {
                    var domain = domainParts[domainParts.Length - 2] + "." + domainParts[domainParts.Length - 1];
                    Response.Cookies.Add(new Cookie("SESSION-ID", SessionId, "/", domain));
                }
                else
                {
                    Response.Cookies.Add(new Cookie("SESSION-ID", SessionId));
                }
            }

            if (Response.Cookies != null)
                foreach (Cookie cookie in Response.Cookies)
                    Response.Headers.Append("Set-Cookie", cookie.ToClientString());

            if (Request.ProtocolVersion >= HttpVersion.Version11)
                Response.Headers.Set("Transfer-Encoding", "chunked");

            #region COMPRESSION HEADERS

            if (Request.Headers["Accept-Encoding"].Contains("gzip"))
                Response.Headers["Content-Encoding"] = "gzip";

            else if (Request.Headers["Accept-Encoding"].Contains("deflate"))
                Response.Headers["Content-Encoding"] = "deflate";

            #endregion

            var h = "HTTP/" + Response.ProtocolVersion + " " + (int)Response.StatusCode + " " +
                Response.StatusDescription + "\r\n" + Response.Headers+"\r\n";

            var b = Response.ContentEncoding.GetBytes(h);
            OutputStream.Write(b, 0, b.Length);
            OutputStream.Flush();

            if (Request.ProtocolVersion >= HttpVersion.Version11)
            {
                OutputStream = new ChunkedStream(OutputStream);
                writer = new StreamWriter(OutputStream, Response.ContentEncoding, 1024);
            }

            #region COMPRESSION STREAM

            if (Request.Headers["Accept-Encoding"].Contains("gzip"))
            {
                OutputStream = new GZipStream(OutputStream, CompressionMode.Compress);
                writer = new StreamWriter(OutputStream, Response.ContentEncoding, 1024);
            }
            else if (Request.Headers["Accept-Encoding"].Contains("deflate"))
            {
                OutputStream = new DeflateStream(OutputStream, CompressionMode.Compress);
                writer = new StreamWriter(OutputStream, Response.ContentEncoding, 1024);
            }

            #endregion

            HeadersSent = true;
        }

        /// <summary>
        ///     Mark the current context has served and free related resources
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
                return;

            if (Engine.DevMode)
                Console.WriteLine(Request.Host + ":" + Request.Url + " - Context closed");

            if (Profiling!=null)
            {
                st.Stop();
                Profiling(Stopwatch.Frequency/st.ElapsedTicks,Request.Host, Request.Url);
            }

            try
            {
                IsOpen = false;
                if (!HeadersSent)
                    SendHeaders();

                Writer.Flush();
                OutputStream.Flush();
                OutputStream.Close();
                Socket.Close();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Save a variable in current session
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="obj">Variable value</param>
        public void Session(string name, object obj)
        {
            if (SessionId == null) SessionId = Security.UID();

            Engine.Sessions.Set(SessionId, name, obj);
        }

        /// <summary>
        ///     Retrieve the related value from current session and cast it to T
        /// </summary>
        /// <param name="name">Variable name</param>
        public T Session<T>(string name)
        {
            if (SessionId == null) SessionId = Security.UID();

            object k = Engine.Sessions.Get(SessionId, name);
            if (k != null)
            {
                return (T) k;
            }
            return default(T);
        }

        /// <summary>
        ///     Retrieve the related value from current session
        /// </summary>
        /// <param name="name">Variable name</param>
        public dynamic Session(string name)
        {
            if (SessionId == null) SessionId = Security.UID();

            return Engine.Sessions.Get(SessionId, name);
        }
    }
}