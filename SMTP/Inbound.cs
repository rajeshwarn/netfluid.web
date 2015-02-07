#region
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
#endregion

namespace NetFluid.SMTP
{
    /// <summary>
    /// Simple SMTP server
    /// </summary>
    public class Inbound
    {
        readonly Dictionary<string, Action<string, SmtpRequest>> verbs;  
        private readonly TcpListener _listener;

        public event Action<SmtpRequest> OnRequestCompleted;
        public Func<MailAddress,bool> MailFrom;

        public string WelcomeMessage;

        public X509Certificate Certificate;

        public Inbound():this(IPAddress.Any,25)
        {
            WelcomeMessage = Environment.MachineName + " NetFluid mail system";
        }

        public Inbound(IPAddress ip, int port)
        {
            _listener = new TcpListener(ip, port);
            _listener.Start();

            WelcomeMessage = Environment.MachineName + " NetFluid mail system";

            verbs = new Dictionary<string, Action<string, SmtpRequest>>();
            verbs.Add("HELO ", (cmd, request) =>
            {
                request.Write("250-Hello " + cmd.Substring("HELO ".Length));
                request.Write("250-AUTH LOGIN PLAIN");
                if (Certificate!=null)
                    request.Write("250 STARTTLS");

            });

            verbs.Add("EHLO ", (cmd, request) =>
            {
                request.Write("250-Hello " + cmd.Substring("HELO ".Length));
                request.Write("250-AUTH LOGIN PLAIN");
                if (Certificate != null)
                    request.Write("250 STARTTLS");

            });

            verbs.Add("STARTTLS", (cmd, request) =>
            {
                request.Write("220 ready when you are");
                SslStream sslStream = new SslStream(request.Stream, true);
                sslStream.AuthenticateAsServer(Certificate, false, SslProtocols.Default, true);
                request.Stream = sslStream;
            });

            verbs.Add("AUTH LOGIN", (cmd, request) =>
            {
                var b64 = cmd.Substring("AUTH LOGIN ".Length);
                var dec = b64.FromBase64();
                Console.WriteLine(dec);
                request.Write("334");
                b64 = request.Read();
                dec = b64.FromBase64();
                Console.WriteLine(dec);
            });

            verbs.Add("AUTH PLAIN",(cmd,request)=>
            {
                request.Write("334");
                var split = request.Read();

                Console.WriteLine(split);
            });

            verbs.Add("MAIL FROM:", (cmd, request) =>
            {
                var add = cmd.Substring("MAIL FROM:".Length).Trim();
                try
                {
                    request.From = new MailAddress(add);

                    if (MailFrom!=null && !MailFrom(request.From))
                    {
                    }

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

                if (OnRequestCompleted!=null)
	            {
                    OnRequestCompleted(request);
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
                            request.Write("220 " +WelcomeMessage);

                            var cmd = request.Read();

                            while (cmd != "QUIT")
                            {
                                if (cmd != null)
                                {
                                    var verb = verbs.Keys.FirstOrDefault(x=>cmd.StartsWith(x,StringComparison.OrdinalIgnoreCase));

                                    if (verb != null)
                                    {
                                        verbs[verb].Invoke(cmd, request);
                                    }
                                    else
                                    {
                                        request.Write("502 5.5.1 Unrecognized command");
                                    }
                                }
                                cmd = request.Read();
                            }
                            request.Write("221 2.0.0 NetFluid.Mail closing connection\r\n");
                            client.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    });
                }
            });
        }
    }
}