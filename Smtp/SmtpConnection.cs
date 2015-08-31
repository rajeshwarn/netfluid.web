#region Header
// Copyright (c) 2013 Hans Wolff
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion



using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netfluid.Smtp
{
    class SmtpConnection
    {
        internal NetworkStream NetworkStream { get; set; }
        internal IPEndPoint RemoteEndPoint { get; set; }
        internal PortListener PortBinding { get; set; }
        internal TcpClient TcpClient { get; set; }
        internal DateTime ConnectionInitiated { get; set; }

        StringReaderStream Reader;
        StreamWriter Writer;

        public event EventHandler<RawLineEventArgs> RawLineReceived = (s, e) => { };
        public event EventHandler<RawLineEventArgs> RawLineSent = (s, e) => { };

        SmtpSession Session { get; set; }
        SmtpServer Server { get; set; }

        internal event EventHandler<SmtpConnectionEventArgs> ClientDisconnected = (s, c) => {};
        internal event EventHandler<SmtpSessionEventArgs> SessionCreated;


        public SmtpConnection(SmtpServer server, PortListener portBinding, TcpClient tcpClient)
        {
            if (server == null) throw new ArgumentNullException("server");
            if (portBinding == null) throw new ArgumentNullException("portBinding");
            if (tcpClient == null) throw new ArgumentNullException("tcpClient");

            Server = server;
            PortBinding = portBinding;
            TcpClient = tcpClient;

            ConnectionInitiated = DateTime.UtcNow;
            NetworkStream = tcpClient.GetStream();
            Reader = new StringReaderStream(NetworkStream);
            Writer = new StreamWriter(NetworkStream) { AutoFlush = true };
            RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            SessionCreated = (sender, args) => Server.Logger.Debug("Session created for " + args.Session.Connection.RemoteEndPoint);
        }

        public TimeSpan GetIdleTime()
        {
            var session = Session;
            return session != null ? session.GetIdleTime() : TimeSpan.Zero;
        }

        public SmtpSession CreateSession()
        {
            var session = new SmtpSession(this);
            session.OnSessionDisconnected +=
                (sender, args) => ClientDisconnected(this, new SmtpConnectionEventArgs(this));
            Session = session;

            SessionCreated(this, new SmtpSessionEventArgs(Session));
            return session;
        }


        public void Disconnect()
        {
            Session.Disconnect();
            try
            {
                NetworkStream.Close();
                TcpClient.Close();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // ignore
            }
        }


        public async Task<byte[]> ReadLineAsync(CancellationToken cancellationToken)
        {
            var rawLine = await Reader.ReadLineAsync(cancellationToken);
            RawLineReceived(this, new RawLineEventArgs(rawLine ?? new byte[0]));
            return rawLine;
        }

        public async Task WriteLineAsyncAndFireEvents(string line)
        {
            if (Writer.BaseStream.CanWrite)
            {
                RawLineSent(this, new RawLineEventArgs(Writer.Encoding.GetBytes(line)));
                await TextWriter.Synchronized(Writer).WriteLineAsync(line);
            }
        }

        #region Dispose
        private bool _disposed;
        private readonly object _disposeLock = new object();

        /// <summary>
        /// Inheritable dispose method
        /// </summary>
        /// <param name="disposing">true, suppress GC finalizer call</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    Disconnect();

                    _disposed = true;
                    if (disposing) GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Free resources being used
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SmtpConnection()
        {
            Dispose(false);
        }
        #endregion
    }
}
