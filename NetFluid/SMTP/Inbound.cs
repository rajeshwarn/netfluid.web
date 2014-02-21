#region
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading.Tasks;
#endregion

namespace NetFluid.SMTP
{
    public class Inbound
    {
        readonly Dictionary<string, Action<string, SmtpRequest>> verbs;  
        private readonly TcpListener _listener;

        public event Action<MailAddress,SmtpRequest> OnMailRecieve;

        public Inbound():this(IPAddress.Any,25)
        {

        }

        public Inbound(IPAddress ip,int port)
        {
            _listener = new TcpListener(ip, port);
            _listener.Start();

            verbs = new Dictionary<string, Action<string, SmtpRequest>>();
            verbs.Add("HELO ", (cmd, request) => request.Write("250 Hello " + cmd.Substring("HELO ".Length)));
            verbs.Add("EHLO ", (cmd, request) => request.Write("250 Hello " + cmd.Substring("EHLO ".Length)));
            verbs.Add("MAIL FROM:", (cmd, request) =>
            {
                var add = cmd.Substring("MAIL FROM:".Length).Trim();
                try
                {
                    request.From = new MailAddress(add);
                    request.Write("250 2.1.0 " + request.From.Address + " Sender ok");
                }
                catch (Exception)
                {
                    request.Write("553 5.5.4 " + add + " invalid address");
                }
            });

            verbs.Add("RCPT TO:", (cmd, request) =>
            {
                var add = cmd.Substring("RCPT TO:".Length);
                try
                {
                    var tmp = new MailAddress(add);
                    var to = new MailAddress(tmp.Address.ToLowerInvariant(),tmp.DisplayName);

                    if (request.To.Contains(to))
                    {
                        request.Write("553 5.5.4 " + add + " recipient already added");
                        return;
                    } 

                    request.To.Add(to);
                    request.Write("250 2.1.5 " + request.From.Address + " recipient  ok");
                }
                catch (Exception)
                {
                    request.Write("553 5.5.4 " + add + " invalid address");
                }
            });

            verbs.Add("DATA", (cmd, request) =>
            {
                request.Write("354 Enter mail, end with \".\" on a line by itself");
                string line;

                var b = request.Blob;
                var toreq = new StreamWriter(b);

                do
                {
                    line = request.Read();
                    if (line != ".")
                        toreq.WriteLine(line);
                } while (line!=".");

                toreq.Flush();
                b.Flush();
                b.Close();

                if (OnMailRecieve!=null)
	            {
                    foreach (var recipient in request.To)
                    {
                        try
                        {
                            OnMailRecieve(recipient, request);
                        }
                        catch (Exception ex)
                        {

                            throw ex;
                        }
                    }
	            }

                request.Write("250 OK");
            });

            verbs.Add("RSET", (x, request) =>
            {
                request.Reset();
                request.Write("250 2.0.0 Reset state");
            });

            verbs.Add("NOOP", (x, request) => request.Write("250 2.0.0 OK"));
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var client = _listener.AcceptTcpClient();
                    client.ReceiveTimeout = 10000;

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var request = new SmtpRequest(client.Client);
                            request.Write("220 " + Environment.MachineName + " NetFluid.Mail");

                            var cmd = request.Read();

                            while (cmd != "QUIT")
                            {
                                if (cmd != null)
                                {
                                    var verb = verbs.Keys.FirstOrDefault(cmd.StartsWith);
                                    if (verb != null)
                                        verbs[verb].Invoke(cmd, request);
                                }
                                cmd = request.Read();
                            }
                            request.Write("221 2.0.0 NetFluid.Mail closing connection\r\n");
                            client.Close();
                        }
                        catch (Exception)
                        {
                        }
                    });
                }
            });
        }
    }
}