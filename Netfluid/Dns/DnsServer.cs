using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Netfluid.Dns
{
    //DNS INTEGRATION, QUERY AND RECORDS FROM http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C

    /// <summary>
    ///  DNS Server and Client
    /// </summary>
    public class DnsServer
    {
        public static IPAddress[] Roots
        {
            get
            {
                return new[]
                {
                    IPAddress.Parse("198.41.0.4"),
                    IPAddress.Parse("192.228.79.201"),
                    IPAddress.Parse("192.33.4.12"),
                    IPAddress.Parse("199.7.91.13"),
                    IPAddress.Parse("192.203.230.10"),
                    IPAddress.Parse("192.5.5.241"),
                    IPAddress.Parse("192.112.36.4"),
                    IPAddress.Parse("128.63.2.53"),
                    IPAddress.Parse("192.36.148.17"),
                    IPAddress.Parse("192.58.128.30"),
                    IPAddress.Parse("193.0.14.129"),
                    IPAddress.Parse("198.32.64.12"),
                    IPAddress.Parse("202.12.27.33")
                };
            }
        }

        /// <summary>
        /// True if DNS Server is running
        /// </summary>
        public bool AcceptingRequest { get; private set; }

        /// <summary>
        /// Implement this function to fill the response
        /// </summary>
        public Func<Request, Response> OnRequest;

        /// <summary>
        /// If true when a local server response is empty it will hask to roots servers
        /// </summary>
        public bool Recursive;

        /// <summary>
        /// Rewrite it to log DNS server events
        /// </summary>
        public Logger Logger;

        IPEndPoint endPoint;
        UdpClient c;

        public DnsServer():this(IPAddress.Any)
        {
        }

        public DnsServer(IPAddress ip, int port=53)
        {
            endPoint = new IPEndPoint(ip, port);
            c = new UdpClient(endPoint);
        }

        /// <summary>
        /// Start syncronosly accepting requests
        /// </summary>
        public void Start()
        {
            AcceptingRequest = true;

            if (OnRequest == null) throw new ArgumentNullException("Assign the OnRequest handler before start the server");

            while (AcceptingRequest)
            {
                try
                {
                    var buffer = c.Receive(ref endPoint);

                    var req = Serializer.ReadRequest(new MemoryStream(buffer));

                    if (OnRequest == null)
                        continue;

                    var resp = OnRequest(req);

                    if(Recursive && resp.Answers.Count == 0 && resp.Authorities.Count==0 && resp.Additionals.Count==0)
                        resp = DnsClient.Query(req, Roots);

                    var r = Serializer.WriteResponse(resp);
                    c.Send(r, r.Length, endPoint);
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    Logger.Error("DNS Server exception " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public void StartAsync()
        {
            Task.Factory.StartNew(() =>Start());
        }

        /// <summary>
        /// Start local DNS server
        /// </summary>
        public void Stop()
        {
            AcceptingRequest = false;
        }
    }
}