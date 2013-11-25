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
using System.Net;
using System.Net.Sockets;

namespace NetFluid
{
    internal class TcpFowarding : IDisposable
    {
        private static Dictionary<string, IPEndPoint> targets;
        private readonly Socket MainSocket;

        private TcpFowarding()
        {
            MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            MainSocket.Close();
        }

        #endregion

        internal static void UnSetFowarding(string host)
        {
            if (targets == null)
                return;

            if (targets.ContainsKey(host))
                targets.Remove(host);
        }

        internal static void SetFowarding(string host, IPEndPoint remote)
        {
            if (targets == null)
                targets = new Dictionary<string, IPEndPoint>();

            if (targets.ContainsKey(host))
                targets[host] = remote;
            else
                targets.Add(host, remote);
        }

        internal static IPEndPoint Fowarded(string host)
        {
            if (targets == null)
                return null;

            IPEndPoint ep;
            targets.TryGetValue(host, out ep);
            return ep;
        }

        internal static void Start(Stream source, byte[] welcome, IPEndPoint remote)
        {
            try
            {
                if (targets == null)
                    targets = new Dictionary<string, IPEndPoint>();

                var destination = new TcpFowarding();
                destination.Connect(remote, source);
                var stream = new NetworkStream(destination.MainSocket);
                var state = new State(source, stream);

                destination.MainSocket.Send(welcome);
                source.BeginRead(state.Buffer, 0, state.Buffer.Length, OnDataReceive, state);
            }
            catch (Exception exception)
            {
                Engine.Logger.Log(LogLevel.Error, "Error during host foward", exception);
            }
        }

        internal static void Start(Stream source, IPEndPoint remote)
        {
            try
            {
                if (targets == null)
                    targets = new Dictionary<string, IPEndPoint>();

                var destination = new TcpFowarding();
                destination.Connect(remote, source);
                var stream = new NetworkStream(destination.MainSocket);
                var state = new State(source, stream);

                source.BeginRead(state.Buffer, 0, state.Buffer.Length, OnDataReceive, state);
            }
            catch (Exception exception)
            {
                Engine.Logger.Log(LogLevel.Error, "Error during host foward", exception);
            }
        }

        private void Connect(EndPoint remoteEndpoint, Stream destination)
        {
            try
            {
                MainSocket.Connect(remoteEndpoint);
                var stream = new NetworkStream(MainSocket);
                var state = new State(stream, destination);
                stream.BeginRead(state.Buffer, 0, state.Buffer.Length, OnDataReceive, state);
            }
            catch (Exception exception)
            {
                Engine.Logger.Log(LogLevel.Error, "Error during host foward", exception);
            }
        }

        private static void OnDataReceive(IAsyncResult result)
        {
            var state = result.AsyncState as State;
            try
            {
                int bytesRead = state.SourceSocket.EndRead(result);
                if (bytesRead > 0)
                {
                    state.DestinationSocket.BeginWrite(state.Buffer, 0, bytesRead, x =>
                    {
                        state.DestinationSocket.
                            EndWrite(x);
                        state.SourceSocket.BeginRead(
                            state.Buffer, 0,
                            state.Buffer.Length,
                            OnDataReceive, state);
                    }, null);
                }
            }
            catch (Exception)
            {
                state.DestinationSocket.Close();
                state.SourceSocket.Close();
            }
        }

        #region Nested type: State

        private class State
        {
            public readonly byte[] Buffer;
            public readonly Stream DestinationSocket;
            public readonly Stream SourceSocket;

            public State(Stream source, Stream destination)
            {
                SourceSocket = source;
                DestinationSocket = destination;
                Buffer = new byte[131072];
            }
        }

        #endregion
    }
}