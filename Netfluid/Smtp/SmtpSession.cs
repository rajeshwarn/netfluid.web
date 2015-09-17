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
using System.Diagnostics;

namespace Netfluid.Smtp
{
    class SmtpSession : IDisposable
    {
        public bool Active { get; protected set; }
        public bool Disconnected { get; protected set; }

        internal SmtpConnection Connection { get; private set; }
        internal event EventHandler<SessionEventArgs> OnSessionDisconnected = (s, e) => { };

        public object Tag { get; set; }

        readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        public SmtpSessionInfo SessionInfo { get; set; }

        public SmtpSession(SmtpConnection connection)
        {
            SessionInfo = new SmtpSessionInfo();
            Active = true;
            Connection = connection;
        }


        public TimeSpan GetIdleTime()
        {
            return Stopwatch.Elapsed;
        }

        public void UpdateActivity()
        {
            Stopwatch.Restart();
        }

        public void Disconnect()
        {
            if (!Disconnected)
            {
                Disconnected = true;
                Active = false;
                if (Connection != null)
                    Connection.Disconnect();
                OnSessionDisconnected(this, new SessionEventArgs(this));
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
        ~SmtpSession()
        {
            Dispose(false);
        }
        #endregion
    }
}
