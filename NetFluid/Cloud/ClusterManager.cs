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
using System.Threading.Tasks;

namespace NetFluid.Cloud
{
    internal class ClusterManager : IClusterManager
    {
        static IPEndPoint RemoteToEndPoint(string remote)
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

        static void Connect(Context cnt,Stream sIn, Stream sOut)
        {
            var buf = new byte[512*1024];
            try
            {
                while (true)
                {
                    int len = sIn.Read(buf, 0, buf.Length);
                    sOut.Write(buf, 0, len);
                }
            }
            catch (Exception)
            {
                sOut.Flush();
                cnt.Close();
            }
        }

        static void Open(Context context,string remote, out Task f, out Task s)
        {
            var destination = new TcpClient { ReceiveTimeout = 200, SendTimeout = 200 };
            var ep = RemoteToEndPoint(remote);
            destination.Connect(ep);
            Stream to = destination.GetStream();

            if (context.Secure)
            {
                var ssl = new SslStream(to);
                ssl.AuthenticateAsClient(remote);
                to = ssl;
            }

            var dbg = context.Request.HttpMethod + " "+ context.Request.RawUrl+ " HTTP/"+context.Request.ProtocolVersion+"\r\n"+ context.Request.Headers+"\r\n";
            var bheader = Encoding.UTF8.GetBytes(dbg);
            to.Write(bheader,0,bheader.Length);
            to.Flush();

            //DESTINATION TO CLIENT

            f = Task.Factory.StartNew(() => Connect(context,to, context.OutputStream));

            // CLIENT TO DESTINATION
            s = Task.Factory.StartNew(() =>Connect(context,context.InputStream,to));
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

            try
            {
                if(Engine.DevMode)
                    Console.WriteLine("Forwarding to "+remote);

                Task f, s;
                Open(context, remote, out f, out s);
                Task.WaitAll(f, s);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return false;
            }
            return true;
        }

        public void Foward(Context context, string remote)
        {
            try
            {
                Task f, s;
                Open(context,remote,out f,out s);
                Task.WaitAll(f, s);
            }
            catch (Exception)
            {
            }
        }
    }
}