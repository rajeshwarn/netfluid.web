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
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NetFluid.HTTP
{
    internal class WebInterface : IDisposable
    {
        private readonly X509Certificate2 certificate;
        private readonly IPEndPoint endpoint;
        private readonly Socket sock;

        public WebInterface(IPAddress addr, int port)
        {
            endpoint = new IPEndPoint(addr, port);
            sock = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(endpoint);
            sock.Listen(512);
        }

        public WebInterface(IPAddress addr, int port, X509Certificate2 certificate)
        {
            endpoint = new IPEndPoint(addr, port);
            sock = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(endpoint);
            sock.Listen(512);
        }

        public WebInterface(IPAddress addr, int port, string certPath)
        {
            certificate = new X509Certificate2(certPath);
            endpoint = new IPEndPoint(addr, port);
            sock = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(endpoint);
            sock.Listen(512);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            sock.Close();
        }

        #endregion

        private static void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            //Console.WriteLine("accepted");
            var listenSocket = sender as Socket;
            do
            {
                try
                {
                    new Context(e.AcceptSocket);
                }
                catch (Exception ex)
                {
                    Engine.Logger.Log(LogLevel.Exception, "Error on context",ex);
                }
                e.AcceptSocket = null;
            } while (!listenSocket.AcceptAsync(e));
        }

        private void OnAcceptCrypt(object sender, SocketAsyncEventArgs e)
        {
            var listenSocket = sender as Socket;
            do
            {
                try
                {
                    new Context(e.AcceptSocket, certificate);
                }
                catch (Exception)
                {
                    Engine.Logger.Log(LogLevel.Exception, "Error on context");
                }
                e.AcceptSocket = null; // to enable reuse
            } while (!listenSocket.AcceptAsync(e));
        }

        public void Start()
        {
            Engine.Logger.Log(LogLevel.Debug,"Starting " + (certificate != null ? "secure " : " ") + "web interface on " + endpoint);

            var e = new SocketAsyncEventArgs();
            if (certificate == null)
            {
                e.Completed += OnAccept;
            }
            else
            {
                e.Completed += OnAcceptCrypt;
            }
            sock.AcceptAsync(e);
        }
    }
}