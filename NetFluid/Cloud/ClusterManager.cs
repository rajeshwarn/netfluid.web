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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetFluid.Cloud
{
    internal class ClusterManager : IClusterManager
    {
        static IPEndPoint RemoteToEndPoint(ref string remote)
        {
            if (!remote.Contains("://"))
                remote = "http://" + remote;

            Uri uri;
            if (Uri.TryCreate(remote,UriKind.Absolute,out uri))
            {
                if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
                    return new IPEndPoint(IPAddress.Parse(uri.Host),uri.Port);

                if (uri.HostNameType == UriHostNameType.Dns)
                {
                    var addr = System.Net.Dns.GetHostAddresses(uri.Host)
                               .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                               .ToArray();

                    if (addr.Length == 0)
                    {
                        Engine.Logger.Log(LogLevel.Error,"Failed to set tcp fowarding to " + remote + ", host not found");
                        return null;
                    }
                    return new IPEndPoint(addr[0],uri.Port);
                }
            }
            Engine.Logger.Log(LogLevel.Error,"Failed to set tcp fowarding to "+remote+", bad remote format");
            return null;
        }

        static void Connect(Stream sIn, Stream sOut)
        {
            var buf = new byte[512*1024];

            while (true)
            {
                int len = sIn.Read(buf, 0, buf.Length);
                sOut.Write(buf, 0, len);
            }
        }

        static bool SocketConnected(Socket s)
        {
            return !(s.Poll(1000, SelectMode.SelectRead) & (s.Available == 0));
        }

        static void Open(Context context,string remote)
        {
            var destination = new TcpClient { ReceiveTimeout = 200, SendTimeout = 200 };
            var ep = RemoteToEndPoint(ref remote);
            destination.Connect(ep);
            Stream to = destination.GetStream();

            if (remote.StartsWith("https:"))
            {
                var ssl = new SslStream(to);
                ssl.AuthenticateAsClient(remote);
                to = ssl;
            }

            if (context.WebSocket)
            {
                #region CONNECT TO SOCKET
                var dbg = context.Request.HttpMethod + " " + context.Request.RawUrl + " HTTP/" + context.Request.ProtocolVersion + "\r\n" + context.Request.Headers + "\r\n";
                var bheader = Encoding.UTF8.GetBytes(dbg);
                to.Write(bheader, 0, bheader.Length);
                to.Flush();

                var f = Task.Factory.StartNew(() =>
                {
                    var buf = new byte[512 * 1024];
                    while (SocketConnected(destination.Client) && SocketConnected(context.Socket))
                    {
                        if (destination.Available > 0)
                        {
                            int len = to.Read(buf, 0, buf.Length > destination.Available ? buf.Length : destination.Available);
                            context.OutputStream.Write(buf, 0, len);
                        }
                    }
                });

                var s = Task.Factory.StartNew(() =>
                {
                    var buf = new byte[512 * 1024];
                    while (SocketConnected(destination.Client) && SocketConnected(context.Socket))
                    {
                        if (context.Socket.Available > 0)
                        {
                            int len = context.InputStream.Read(buf, 0, buf.Length > context.Socket.Available ? buf.Length : context.Socket.Available);
                            to.Write(buf, 0, len);
                        }
                    }
                });
                Task.WaitAny(f, s);
                #endregion
                return;
            }

            var request = WebRequest.Create(new Uri(new Uri(remote),context.Request.RawUrl)) as HttpWebRequest;
            if(request==null)
                return;

            request.Method = context.Request.HttpMethod;
            request.AutomaticDecompression = DecompressionMethods.GZip;

            context.Request.Headers.ForEach(header => header.ToArray().ForEach(value =>
            {
                try
                {
                    switch (header.Name.ToLowerInvariant())
                    {
                        case "user-agent":
                            request.UserAgent = header.Value;
                        break;
                        case "connection":
                            request.KeepAlive = header.Value == "Keep-Alive";
                        break;
                        case "host":
                            request.Host = "www.facebook.com";
                            //request.Host = header.Value;
                        break;
                        case "accept":
                            request.Accept = header.Value;
                        break;
                        case "referer":
                            request.Referer = header.Value;
                        break;
                        case "content-type":
                            request.ContentType = header.Value;
                        break;
                        case "content-length":
                            request.ContentLength = long.Parse(header.Value);
                        break;
                        default:
                            request.Headers.Add(header.Name, value);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }));

            switch (context.Request.HttpMethod.ToLowerInvariant())
            {
                case "post":
                case "put":
                    context.InputStream.CopyTo(request.GetRequestStream(),context.Request.ContentLength);
                break;
            }

            int test = 0;
            retry:
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                foreach (Cookie cookie in response.Cookies)
                    context.Response.Cookies.Add(cookie);

                context.Response.ContentType = response.ContentType;
                context.Response.StatusCode = (StatusCode)response.StatusCode;

                context.SendHeaders();
                response.GetResponseStream().CopyTo(context.OutputStream);
            }
            catch (Exception)
            {
                test++;

                if (test<=5)
                    goto retry;
                throw;
            }

        }

        private readonly Dictionary<string, string> remotes;

        public ClusterManager()
        {
            remotes = new Dictionary<string, string>();
        }

        public void AddFowarding(string host, string remote)
        {
            remotes.Add(host,remote);
        }

        public void RemoveFowarding(string host)
        {
            remotes.Remove(host);
        }

        public bool Handle(Context context)
        {
            string remote;

            if (!remotes.TryGetValue(context.Request.Host, out remote)) return false;

            if (Engine.DevMode)
                Console.WriteLine("Forwarding to " + remote);

            Foward(context, remote);
            return true;
        }

        public void Foward(Context context, string remote)
        {
            try
            {
                Open(context,remote);
            }
            catch (Exception exception)
            {
                Engine.Logger.Log(LogLevel.Exception,"Error forwarding context to "+remote,exception);
            }
        }
    }
}