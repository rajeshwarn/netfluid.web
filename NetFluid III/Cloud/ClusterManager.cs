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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Cloud
{
    class ClusterManager:IClusterManager
    {
        class State
        {
            public Stream Source { get; private set; }
            public Stream Destination { get; private set; }
            public byte[] InBuffer { get; private set; }
            public byte[] OutBuffer { get; private set; }

            public State(Stream source, Stream destination, byte[] welcome, int size)
            {
                Source = source;
                Destination = destination;
                InBuffer = new byte[65536];
                OutBuffer = new byte[65536];

                destination.BeginWrite(welcome, 0, size, Inbound, null);
                Outbound(null);
            }

            private void Inbound(IAsyncResult result)
            {
                try
                {
                    Source.BeginRead(InBuffer, 0, InBuffer.Length, x =>
                    {
                        var k = Source.EndRead(x);
                        Destination.BeginWrite(InBuffer, 0, k, Inbound, null);
                    }, null);
                }
                catch (Exception)
                {
                    ClusterManager.Remove(this);
                }
            }
            private void Outbound(IAsyncResult result)
            {
                try
                {
                    Destination.BeginRead(OutBuffer, 0, OutBuffer.Length, x =>
                    {
                        var k = Destination.EndRead(x);
                        Source.BeginWrite(OutBuffer, 0, k, Outbound, null);
                    }, null);
                }
                catch (Exception)
                {
                    ClusterManager.Remove(this);
                }
            }
        }

        static ConcurrentBag<State> States;
        static ConcurrentDictionary<string, IPEndPoint> Targets;

        static ClusterManager()
        {
            States = new ConcurrentBag<State>();
            Targets = new ConcurrentDictionary<string, IPEndPoint>();
        }

        static void Remove(State state)
        {
            ClusterManager.States.TryTake(out state);
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
                IPAddress[] addr = Dns.GetHostAddresses(remote).Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray();

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

            if (fow != null)
            {
                Task.Factory.StartNew(()=>
                {
                    var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.ReceiveTimeout = 5000;
                    sock.SendTimeout = 5000;
                    sock.Connect(fow);
                    Add(new State(context.InputStream, new NetworkStream(sock), context.Buffer, context.Buffer.Length));
                });

                return true;
            }
            return false;
        }
    }
}
