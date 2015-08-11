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
    /// SMTP server for just in-bound comunication, relay is not directly allowed
    /// </summary>
    public class Inbound
    {
        readonly Dictionary<string, Action<string, SmtpRequest>> verbs;  
        private readonly TcpListener _listener;

        /// <summary>
        /// Called when the client complete is request. To relay the message of foward it to the recipient you must fill this function
        /// </summary>
        public event Action<SmtpRequest> OnRequestCompleted;

        /// <summary>
        /// Called when the client invoke a MAILTO command. If return false the client cannot send the email to the specified recipient
        /// </summary>
        public Func<MailAddress,bool> MailFrom;

        /// <summary>
        /// Called when the client send an AUTH command. To implement authetication in your SMTP server you must fill this function.
        /// </summary>
        public Func<SmtpRequest, bool> Authenticate;

        /// <summary>
        /// Get and set the welcome messagge of the server (default value:Environment.MachineName + " NetFluid SMTP inbound connection system")
        /// </summary>
        public string WelcomeMessage;

        /// <summary>
        /// SSH certificate. To enable SMTPS you must fill this field
        /// </summary>
        public X509Certificate Certificate;

        /// <summary>
        /// Instance a new SMTP inbound connector on every IP address of current system
        /// </summary>
        public Inbound():this(IPAddress.Any,25)
        {
            WelcomeMessage = Environment.MachineName + " NetFluid SMTP inbound connection system";
        }

        /// <summary>
        /// Instance a new SMTP inbound connector on a specific IP address of current system
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
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
                if (cmd=="AUTH LOGIN")
                {
                    //ask username
                    request.Write("334 VXNlcm5hbWU6");
                    var b64 = request.Read();
                    request.Username = b64.FromBase64();

                    //ask password
                    request.Write("334 UGFzc3dvcmQ6");
                    b64 = request.Read();
                    request.Password = b64.FromBase64();
                }
                else
                {
                    //username already provided, ask password
                    var b64 = cmd.Substring("AUTH LOGIN ".Length);
                    request.Username = b64.FromBase64();

                    request.Write("334 UGFzc3dvcmQ6");
                    b64 = request.Read();
                    request.Password = b64.FromBase64();
                }

                if (Authenticate!=null && !Authenticate(request))
                {
                    request.Write("535 authentication failed");
                    return;
                }
                request.Write("235 Authentication successful");
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

        /// <summary>
        /// The SMTP inbound connector starts to accept clients and serve responses
        /// </summary>
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