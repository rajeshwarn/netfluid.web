using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetFluid.DNS;
using NetFluid.DNS.Records;

namespace NetFluid
{
    //DNS INTEGRATION, QUERY AND RECORDS FROM http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C

    /// <summary>
    ///     Provide methods for DNS query. Server cooming soon
    /// </summary>
    public static class Dns
    {
        /// <summary>
        /// Executed when Local DNS Server recieve a request
        /// </summary>
        public static event Func<Request, Response> OnRequest;

        /// <summary>
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public static void StartAcceptRequest(IPAddress ip)
        {
            EndPoint endPoint = new IPEndPoint(ip, 53);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(endPoint);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var buffer = new byte[65000];
                        socket.ReceiveFrom(buffer, ref endPoint);
                        var req = Serializer.ReadRequest(new MemoryStream(buffer));

                        if (OnRequest == null)
                            continue;

                        var resp = OnRequest(req);
                        socket.SendTo(Serializer.WriteResponse(resp), endPoint);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
            });
        }

        /// <summary>
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public static void StartAcceptRequest(string ip)
        {
            StartAcceptRequest(IPAddress.Parse(ip));
        }

        /// <summary>
        /// Ask a DNS question to the specified server
        /// </summary>
        /// <param name="question">Question</param>
        /// <param name="server">Server</param>
        /// <returns></returns>
        public static Record[] Query(Question question, IPAddress server)
        {
            return Query(question.QName,question.QType,question.QClass, new[] { server });
        }

        /// <summary>
        /// Ask a DNS question  to a specific server
        /// </summary>
        public static Record[] Query(string name, QType qtype, QClass qclass, string server)
        {
            return Query(name, qtype, qclass, IPAddress.Parse(server));
        }

        /// <summary>
        /// Ask a DNS question  to a specific server
        /// </summary>
        public static Record[] Query(string name, QType qtype, QClass qclass, IPAddress server)
        {
            return Query(name, qtype, qclass, new[] {server});
        }

        /// <summary>
        /// Ask a DNS question to a specific server
        /// </summary>
        public static Record[] Query(string name, QType qtype, QClass qclass = QClass.IN, IPAddress[] servers = null)
        {
            if (servers == null)
                servers = Network.Dns;

            var question = new Question(name, qtype, qclass);
            var request = new Request {question};

            // RFC1035 max. size of a UDP datagram is 512 bytes
            var responseMessage = new byte[512];

            foreach (var ip in servers)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.ReceiveTimeout = 200;
                socket.SendTimeout = 200;

                EndPoint endPoint = new IPEndPoint(ip, 53);

                try
                {
                    socket.SendTo(request.Write, endPoint);
                    int intReceived = socket.Receive(responseMessage);
                    var data = new byte[intReceived];
                    Array.Copy(responseMessage, data, intReceived);

                    var resp = Serializer.ReadResponse(data);
                    if (resp.Answers.Count > 0)
                        return resp.Records;
                }
                catch (SocketException)
                {
                }
                finally
                {
                    socket.Close();
                }
            }
            return new Record[0];
        }

        /// <summary>
        /// Query A records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> A(string name)
        {
            return Query(name, QType.A).Select(x => x.ToString());
        }

        /// <summary>
        /// Query AAAA records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> AAAA(string name)
        {
            return Query(name, QType.AAAA).Select(x => x.ToString());
        }

        /// <summary>
        /// Query CNAME records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> CNAME(string name)
        {
            return Query(name, QType.CNAME).Select(x => x.ToString());
        }

        /// <summary>
        /// Query MX records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> MX(string name)
        {
            return Query(name, QType.MX).Select(x => x.ToString());
        }

        /// <summary>
        /// Query NS records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> NS(string name)
        {
            return Query(name, QType.NS).Select(x => x.ToString());
        }

        /// <summary>
        /// Query PTR records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> PTR(string name)
        {
            return Query(name, QType.PTR).Select(x => x.ToString());
        }

        /// <summary>
        /// Query SOA records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> SOA(string name)
        {
            return Query(name, QType.SOA).Select(x => x.ToString());
        }

        /// <summary>
        /// Query TXT records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> TXT(string name)
        {
            return Query(name, QType.TXT).Select(x => x.ToString());
        }
    }
}