using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Netfluid.Dns;
using Netfluid.Dns.Records;

namespace Netfluid.Dns
{
    //DNS INTEGRATION, QUERY AND RECORDS FROM http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C

    /// <summary>
    ///  DNS Server and Client
    /// </summary>
    public class DnsServer
    {
        /// <summary>
        /// True if DNS Server is running
        /// </summary>
        public bool AcceptingRequest { get; private set; }

        /// <summary>
        /// Executed when Local DNS Server recieve a request
        /// </summary>
        public Func<Request, Response> OnRequest;

        public Logger Logger;

        public IPAddress[] Roots
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
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public void StartAcceptRequest(IPAddress ip)
        {
            if (AcceptingRequest)
                return;

            AcceptingRequest = true;
            var endPoint = new IPEndPoint(ip, 53);
            var c = new UdpClient(endPoint);

            Task.Factory.StartNew(() =>
            {
                while (AcceptingRequest)
                {
                    try
                    {
                        var buffer = c.Receive(ref endPoint);

                        var req = Serializer.ReadRequest(new MemoryStream(buffer));

                        if (OnRequest == null)
                            continue;

                        var resp = OnRequest(req);

                        var r = Serializer.WriteResponse(resp);
                        c.Send(r, r.Length, endPoint);
                    }
                    catch(SocketException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("DNS Server exception "+  ex.Message);
                    }
                }
            });
        }

        /// <summary>
        /// Start local DNS server
        /// </summary>
        public void StopAcceptiongRequest()
        {
            AcceptingRequest = false;
        }

        /// <summary>
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public void StartAcceptRequest(string ip)
        {
            StartAcceptRequest(IPAddress.Parse(ip));
        }
    }
}