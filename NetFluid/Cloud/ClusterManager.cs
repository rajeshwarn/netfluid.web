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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace NetFluid.Cloud
{
    class ClusterManager:IClusterManager
    {
        class State : IDisposable
        {
            private Context Source { get; set; }
            private TcpClient Destination { get; set; }
            private NetworkStream DestinationStream { get; set; }
            private byte[] InBuffer { get; set; }
            private byte[] OutBuffer { get; set; }

            public State(Context source, IPEndPoint fow)
            {
                Source = source;

                Destination = new TcpClient {ReceiveTimeout = 100, SendTimeout = 100};
                Destination.Connect(fow);
                DestinationStream = Destination.GetStream();

                InBuffer = new byte[256];
                OutBuffer = new byte[256];

                DestinationStream.BeginWrite(source.Buffer, 0, source.Buffer.Length, Inbound, null);
                Outbound(null);
            }

            void TryClose()
            {
                try
                {
                    Source.Close();
                }
                catch (Exception)
                {
                }

                try
                {
                    Destination.Close();
                }
                catch (Exception)
                {
                }

                Source = null;
                Destination = null;
                DestinationStream = null;
                InBuffer = null;
                OutBuffer=null;

                Remove(this);
            }

            private void Inbound(IAsyncResult result)
            {
                try
                {
                    if (!Source.Socket.Connected || !Destination.Connected)
                        return;

                    Source.InputStream.BeginRead(InBuffer, 0, InBuffer.Length, x =>
                    {
                        try
                        {
                            var k = Source.InputStream.EndRead(x);

                            if (k==0)
                                return;

                            DestinationStream.BeginWrite(InBuffer, 0, k, Inbound, null);
                        }
                        catch (Exception)
                        {
                            TryClose();
                        }
                    }, null);
                }
                catch (Exception)
                {
                    TryClose();
                }
            }
            private void Outbound(IAsyncResult result)
            {
                try
                {
                    if (!Destination.Connected || !Source.Socket.Connected)
                        return;

                    DestinationStream.BeginRead(OutBuffer, 0, OutBuffer.Length, x =>
                    {
                        try
                        {
                            var k = DestinationStream.EndRead(x);

                            if (k == 0)
                                return;

                            Source.OutputStream.BeginWrite(OutBuffer, 0, k, Outbound, null);
                        }
                        catch (Exception)
                        {
                            TryClose();
                        }
                    }, null);
                }
                catch (Exception)
                {
                    TryClose();
                }
            }

            public void Dispose()
            {
                TryClose();
            }
        }

        static readonly ConcurrentBag<State> States;
        static readonly ConcurrentDictionary<string, IPEndPoint> Targets;

        static ClusterManager()
        {
            States = new ConcurrentBag<State>();
            Targets = new ConcurrentDictionary<string, IPEndPoint>();
        }

        static void Remove(State state)
        {
            States.TryTake(out state);
        }

        static void Add(State state)
        {
            States.Add(state);
        }

        public void AddFowarding(string host, string remote)
        {
            IPAddress ip;
            int port = 80;

            if (remote.Contains(':'))
            {
                if (!int.TryParse(remote.Substring(remote.LastIndexOf(':') + 1), out port))
                    port = 80;

                remote = remote.Substring(0, remote.LastIndexOf(':'));
            }

            if (!IPAddress.TryParse(remote, out ip))
            {
                var addr = System.Net.Dns.GetHostAddresses(remote).Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray();

                if (addr.Length == 0)
                    throw new Exception("Host " + remote + " not found");

                ip = addr[0];
            }

            Targets.TryAdd(host, new IPEndPoint(ip, port));
        }

        public void RemoveFowarding(string host)
        {
            IPEndPoint ip;
            Targets.TryRemove(host, out ip);
        }

        public bool Handle(Context context)
        {
            IPEndPoint fow;
            Targets.TryGetValue(context.Request.Host, out fow);

            if (fow == null)
                return false;
            
            /*var destination = new TcpClient { ReceiveTimeout = 100, SendTimeout = 100 };
            destination.Connect(fow);
                
            var to = destination.GetStream();
            to.Write(context.Buffer,0,context.Buffer.Length);
                
            var buffer = new byte[1024];

            while (destination.Connected && context.Socket.Connected)
            {
                if (destination.Available>0)
                {
                    var r = to.Read(buffer, 0, destination.Available > buffer.Length ? buffer.Length: destination.Available);
                    context.OutputStream.Write(buffer,0,r);
                }
                if (context.Socket.Available > 0)
                {
                    var r = context.InputStream.Read(buffer, 0, context.Socket.Available > buffer.Length ? buffer.Length : context.Socket.Available);
                    to.Write(buffer, 0, r);
                }
            }

            destination.Close();
            context.Close();*/

            Add(new State(context,fow));
            return true;
        }
    }
}
