#region Header
// Copyright (c) 2013-2015 Hans Wolff
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


using Netfluid.Smtp.Config;
using System.Net.Mail;
using Netfluid.Net;

namespace Netfluid.Smtp
{
    public class SmtpServer : IDisposable
    {
        public string Greeting { get; set; }

        ConcurrentBag<TcpService> Bindings { get; set; }
        ConcurrentDictionary<EndPoint, SmtpConnection> Connections { get; set; }
        public SmtpServerConfiguration Configuration { get; set; }

        internal event EventHandler<SmtpConnectionEventArgs> ClientConnected;
        internal event EventHandler<SmtpConnectionEventArgs> ClientDisconnected;

        public Func<SmtpSessionInfo, MailAddress, SmtpResponse> VerifyRecipientTo { get; set; }
        public Func<SmtpSessionInfo, MailAddress, SmtpResponse> VerifyMailFrom { get; set; }
        public Action<SmtpSessionInfo> RequestCompleted { get; set; }

        public Logger Logger { get; set; }

        IdleConnectionDisconnectWatchdog Watchdog { get; set; }

        public SmtpServer()
        {
            Bindings = new ConcurrentBag<TcpService>();
            Connections = new ConcurrentDictionary<EndPoint, SmtpConnection>();
            Configuration = new SmtpServerConfiguration();
            Watchdog = new IdleConnectionDisconnectWatchdog(this);
            Logger = new NullLogger();

            ClientConnected = (sender, args) => Logger.Info("Client connected from " + args.Connection.RemoteEndPoint);
            ClientDisconnected = (sender, args) => Logger.Info("Client disconnected from " + args.Connection.RemoteEndPoint);
        }

        public static SmtpServer CreateAndBind(IPAddress serverListenAddress, int port)
        {
            var smtpServer = new SmtpServer();
            smtpServer.BindAndListenTo(serverListenAddress, port);
            return smtpServer;
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
                    var oldBindings = Bindings;
                    Bindings = new ConcurrentBag<TcpService>();

                    StopListenTo(oldBindings);

                    _disposed = true;
                    if (disposing) GC.SuppressFinalize(this);
                }
            }
        }

        private static void StopListenTo(IEnumerable<TcpService> bindings)
        {
            if (bindings == null) return;

            foreach (var binding in bindings)
            {
                binding.StopListen();
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
        ~SmtpServer()
        {
            Dispose(false);
        }

        #endregion

        internal IEnumerable<SmtpConnection> GetConnections()
        {
            return Connections.Values;
        }

        private readonly object _bindAndListenLock = new object();

        public TcpService BindAndListenTo(IPAddress serverListenAddress, int serverPort)
        {
            lock (_bindAndListenLock)
            {
                StartWatchdogOnFirstCall();

                var portBinding = CreateNewPortBindingAndStartListen(serverListenAddress, serverPort);
                Bindings.Add(portBinding);
                return portBinding;
            }
        }

        private void StartWatchdogOnFirstCall()
        {
            if (Bindings.Count == 0)
                Watchdog.Start();
        }

        private TcpService CreateNewPortBindingAndStartListen(IPAddress serverListenAddress, int serverPort)
        {
            var portBinding = new TcpService(serverListenAddress ?? IPAddress.Any, serverPort);
            portBinding.ClientConnected += PortBindingClientConnected;
            portBinding.StartListen();
            Logger.Info(String.Format("Started listening to {0}:{1}", serverListenAddress, serverPort));
            return portBinding;
        }

        private async void PortBindingClientConnected(TcpService serverPortBinding, TcpClient newConnectedTcpClient)
        {
            var connection = new SmtpConnection(this, serverPortBinding, newConnectedTcpClient);
            connection.ClientDisconnected += (sender, args) => ClientDisconnected(this, new SmtpConnectionEventArgs(args.Connection));

            Connections[connection.RemoteEndPoint] = connection;
            ClientConnected(this, new SmtpConnectionEventArgs(connection));

            await CreateSessionAndProcessCommands(connection);
        }

        private async Task CreateSessionAndProcessCommands(SmtpConnection connection)
        {
            var session = connection.CreateSession();
            await SetupSessionThenProcessCommands(connection, session);
        }

        private async Task SetupSessionThenProcessCommands(SmtpConnection connection, SmtpSession session)
        {
            await SendGreetingAsync(connection, Greeting ?? Configuration.DefaultGreeting);

            var sessionInfoParseResponder = new SmtpCommandParser(this,session.SessionInfo);

            var rawLineDecoder = new RawLineDecoder(connection);
            rawLineDecoder.RequestDisconnection += (s, e) =>
            {
                if (!e.DisconnectionExpected)
                {
                    Logger.Warn(String.Format("Connection unexpectedly lost {0}", connection.RemoteEndPoint));
                }

                rawLineDecoder.Cancel();
                session.Disconnect();
            };
            rawLineDecoder.DetectedActivity += (s, e) => session.UpdateActivity();
            rawLineDecoder.ProcessLineCommand += async (s, e) =>
            {
                var response = sessionInfoParseResponder.ProcessLineCommand(e.Buffer);
                if (response == null || !response.HasValue) return;

                if (response.ResponseCode == SmtpResponse.DisconnectResponseCode)
                {
                    await SendResponseAsync(connection, response);

                    Logger.Debug(String.Format("Remote connection disconnected {0}", connection.RemoteEndPoint));
                    rawLineDecoder.Cancel();
                    await Task.Delay(100).ContinueWith(t => session.Disconnect());
                    return;
                }

                await SendResponseAsync(connection, response);
            };

#pragma warning disable 4014
            rawLineDecoder.ProcessCommandsAsync();
#pragma warning restore 4014
        }

        private async Task SendResponseAsync(SmtpConnection connection, SmtpResponse response)
        {
            LogResponse(response);

            foreach (var additional in response.AdditionalLines)
                await connection.WriteLineAsyncAndFireEvents(additional);

            await connection.WriteLineAsyncAndFireEvents(response.ResponseCode + " " + response.ResponseText);
        }

        private void LogResponse(SmtpResponse response)
        {
            if (Logger.LogLevel < LogLevel.Debug) return;

            var logMessage = new StringBuilder();
            foreach (var additionalLine in response.AdditionalLines)
            {
                logMessage.AppendLine(">>> " + additionalLine);
            }

            logMessage.AppendLine(">>> " + response.ResponseCode + " " + response.ResponseText);
            Logger.Debug(logMessage.ToString());
        }


        async Task SendGreetingAsync(SmtpConnection connectionToSendGreetingTo, string greeting)
        {
            if (connectionToSendGreetingTo == null) throw new ArgumentNullException("connectionToSendGreetingTo");

            var greetingLine = "220 " + greeting;

            Logger.Debug(">>> " + greetingLine);
            await connectionToSendGreetingTo.WriteLineAsyncAndFireEvents(greetingLine);
        }
    }
}
