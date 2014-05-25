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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

        private const int BufferSize = 16384;
        private static ConcurrentBag<Tuple<long, string>> _profilingResults;
        private static bool _profiling;

        private readonly MemoryStream _ms;
        private readonly Stopwatch st;

        /// <summary>
        ///     Temporary buffer for incoming headers data.
        ///     Internally used for connection fowarding
        /// </summary>
        internal byte[] Buffer;

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

        private int _position;
        private int readBytes;
        private StreamReader reader;
        private string sessionId;
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
            readBytes = 0;

            if (_profiling)
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

            Buffer = new byte[BufferSize];
            _ms = new MemoryStream(BufferSize);
            _position = 0;
            InputStream.BeginRead(Buffer, 0, BufferSize, OnRead, this);
        }

        /// <summary>
        ///     Initialize HTTPS context from client socket, reading headers and post data
        /// </summary>
        /// <param name="sock">Client socket</param>
        /// <param name="certificate">PFX Certificate</param>
        public Context(Socket sock, X509Certificate2 certificate)
        {
            if (_profiling)
            {
                st = new Stopwatch();
                st.Start();
            }


            Socket = sock;
            Secure = true;

            _ms = new MemoryStream();
            _position = 0;

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

                    Buffer = new byte[BufferSize];
                    InputStream.BeginRead(Buffer, 0, BufferSize, OnRead, this);
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
        ///     True if the context has been created with HTTPS
        /// </summary>
        public bool Secure { get; private set; }

        /// <summary>
        ///     True if the context is not even served
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
        ///     If true the application server will collect statistical data about performances
        /// </summary>
        public static bool Profiling
        {
            get { return _profiling; }
            set
            {
                _profilingResults = new ConcurrentBag<Tuple<long, string>>();
                _profiling = value;
            }
        }

        /// <summary>
        ///     If Profiling is "on" return milliseconds used to serve each single uri
        /// </summary>
        public static IEnumerable<Tuple<long, string>> Profile
        {
            get { return _profilingResults; }
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

        /// <summary>
        ///     Read request headers
        /// </summary>
        private void OnRead(IAsyncResult ares)
        {
            int nread;

            try
            {
                nread = InputStream.EndRead(ares);
            }
            catch (Exception)
            {
                return;
            }

            _ms.Write(Buffer, 0, nread);

            readBytes += nread;

            if (_ms.Length > 32768)
            {
                Response.StatusCode = StatusCode.BadRequest;
                Close();
                return;
            }

            if (nread == 0)
            {
                Socket.Close();
                return;
            }

            byte[] t = _ms.GetBuffer();
            string header = ReadHeaders(t, ref _position, t.Length - _position);

            if (header == "")
            {
                try
                {
                    InputStream.BeginRead(Buffer, 0, Buffer.Length, OnRead, this);
                }
                catch
                {
                }

                return;
            }


            var lines = header.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

            #region FIRST LINE

            string[] parts = lines[0].Split(Separators, 3);
            if (parts.Length >= 3)
            {
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
                            Response.ProtocolVersion = Request.ProtocolVersion = Version.Parse(parts[2].Substring("HTTP/".Length));
                        break;
                    }
                }
                catch (Exception)
                {
                    Response.ProtocolVersion = Version.Parse("0.0");
                }

                Request.Get = new QueryValueCollection();
                int index = parts[1].IndexOf('?');

                if (index >= 0)
                {
                    Request.Url = HttpUtility.UrlDecode(parts[1].Substring(0, index));
                    parts[1] = parts[1].Substring(index + 1);

                    parts[1].Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries).ForEach(kv =>
                    {
                        int pos = kv.IndexOf('=');

                        var val = pos < 0 ? "true" : HttpUtility.UrlDecode(kv.Substring(pos + 1));
                        var key = pos < 0 ? HttpUtility.UrlDecode(kv) : HttpUtility.UrlDecode(kv.Substring(0, pos));

                        Request.Get.Add(key, val);
                    });
                }
                else
                {
                    Request.Url = HttpUtility.UrlDecode(Request.RawUrl);
                }
            }
            else
            {
                Response.StatusCode = StatusCode.BadRequest;
            }

            #endregion

            for (int i = 1; i < lines.Length; i++)
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

            if (Engine.Cluster.Handle(this))
                return;

            if (Request.HttpMethod != "GET" && Buffer.Length != _position)
            {
                Stream fs;
                try_again:

                try
                {
                    fs = new FileStream(Path.GetFullPath(Path.Combine(Path.GetTempPath(), Security.UID())),FileMode.OpenOrCreate);
                    fs.Write(t, _position, readBytes - _position);
                }
                catch (Exception)
                {
                    goto try_again;
                }
                ReadAndSave(readBytes - _position, Request.ContentLength, fs);
                return;
            }
            Engine.Serve(this);
        }

        /// <summary>
        ///     OnRead has been completed but some recieved bytes belongs to request body (post data)
        /// </summary>
        private void ReadAndSave(long read, long total, Stream s)
        {
            if (read == total)
            {
                s.Flush();
                s.Seek(0, SeekOrigin.Begin);

                if (Request.Headers["Content-Type"].StartsWith("application/x-www-form-urlencoded"))
                    PostManager.DecodeUrl(this, s);
                else if (Request.Headers["Content-Type"].StartsWith("multipart/form-data; boundary="))
                    PostManager.DecodeMultiPart(this, s);
                else
                    InputStream = s;
                Engine.Serve(this);
                return;
            }

            var b = new byte[4096];
            var n = (int) (total - read);

            if (n > b.Length)
                n = b.Length;

            if (n < 0)
            {
                s.Flush();
                s.Seek(0, SeekOrigin.Begin);

                if (Request.Headers["Content-Type"].StartsWith("application/x-www-form-urlencoded"))
                    PostManager.DecodeUrl(this, s);
                else if (Request.Headers["Content-Type"].StartsWith("multipart/form-data; boundary="))
                    PostManager.DecodeMultiPart(this, s);
                else
                    InputStream = s;
                Engine.Serve(this);
                return;
            }

            InputStream.BeginRead(b, 0, n, x =>
            {
                try
                {
                    int r = InputStream.EndRead(x);
                    read += r;
                    s.Write(b, 0, r);

                    if (read < total)
                    {
                        ReadAndSave(read, total, s);
                    }
                    else
                    {
                        s.Flush();
                        s.Seek(0, SeekOrigin.Begin);

                        if (
                            Request.Headers["Content-Type"].StartsWith("application/x-www-form-urlencoded"))
                            PostManager.DecodeUrl(this, s);
                        else if (
                            Request.Headers["Content-Type"].StartsWith("multipart/form-data; boundary="))
                            PostManager.DecodeMultiPart(this, s);
                        else
                            InputStream = s;
                        Engine.Serve(this);
                    }
                }
                catch
                {
                }
            }, null);
        }

        /// <summary>
        ///     Dectect if OnRead has finished
        /// </summary>
        private string ReadHeaders(IList<byte> b, ref int offset, int len)
        {
            try
            {
                int last = offset + len;
                for (int i = offset; i < last; i++)
                    if (i > 4 && b[i] == 10 && b[i - 1] == 13 && b[i - 2] == 10 && b[i - 3] == 13)
                    {
                        offset = i + 1;
                        return Request.ContentEncoding.GetString(Buffer, 0, i + 1);
                    }
                return string.Empty;
            }
            catch
            {
            }
            return string.Empty;
        }

        /// <summary>
        ///     Send Response headers to the client
        /// </summary>
        public void SendHeaders()
        {
            try
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
                if (!Response.KeepAlive || Response.StatusCode >= (StatusCode) 400)
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
                    Response.Cookies.Add(new Cookie("SESSION-ID", SessionId));
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

                string h = string.Format("HTTP/{0} {1} {2}\r\n{3}\r\n", Response.ProtocolVersion,
                    (int) Response.StatusCode,
                    Response.StatusDescription, Response.Headers);
                byte[] b = Response.ContentEncoding.GetBytes(h);
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
            catch (Exception ex)
            {
            }
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

            if (_profiling)
            {
                st.Stop();
                _profilingResults.Add(new Tuple<long, string>(Stopwatch.Frequency/st.ElapsedTicks,
                    Request.Host + Request.Url));
            }


            IsOpen = false;
            if (!HeadersSent)
                SendHeaders();


            try
            {
                Writer.Flush();
                OutputStream.Flush();
            }
            catch
            {
            }


            OutputStream.Close();

            Socket.Close();
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