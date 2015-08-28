using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Netfluid.DNS;
using Netfluid.DNS.Records;

namespace Netfluid
{
    //DNS INTEGRATION, QUERY AND RECORDS FROM http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C

    /// <summary>
    ///  DNS Server and Client
    /// </summary>
    public static class Dns
    {
        /// <summary>
        /// True if DNS Server is running
        /// </summary>
        public static bool AcceptingRequest { get; private set; }

        /// <summary>
        /// Executed when Local DNS Server recieve a request
        /// </summary>
        public static Func<Request, Response> OnRequest;

        public static ILogger Logger;

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
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public static void StartAcceptRequest(IPAddress ip)
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
                        Logger.Log("DNS Server exception", ex);
                    }
                }
            });
        }

        /// <summary>
        /// Start local DNS server
        /// </summary>
        public static void StopAcceptiongRequest()
        {
            AcceptingRequest = false;
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
        /// Ask a DNS question to system DNS server
        /// </summary>
        /// <param name="question">Question</param>
        /// <param name="server">Server</param>
        /// <returns></returns>
        public static Response Query(Question question)
        {
            return Query(question.QName, question.QType, question.QClass, AcceptingRequest ? Network.Dns.Where(x=>!x.IsLocal()) : Network.Dns);
        }


        /// <summary>
        /// Ask a DNS question to the specified server
        /// </summary>
        /// <param name="question">Question</param>
        /// <param name="server">Server</param>
        /// <returns></returns>
        public static Response Query(Question question, string server)
        {
            return Query(question.QName, question.QType, question.QClass, new[] { IPAddress.Parse(server) });
        }

        /// <summary>
        /// Ask a DNS question to the specified server
        /// </summary>
        /// <param name="question">Question</param>
        /// <param name="server">Server</param>
        /// <returns></returns>
        public static Response Query(Question question, IPAddress server)
        {
            return Query(question.QName,question.QType,question.QClass, new[] { server });
        }

        /// <summary>
        /// Ask a DNS question  to a specific server
        /// </summary>
        public static Response Query(string name, QType qtype, QClass qclass, string server)
        {
            return Query(name, qtype, qclass, IPAddress.Parse(server));
        }

        /// <summary>
        /// Ask a DNS question  to a specific server
        /// </summary>
        public static Response Query(string name, QType qtype, QClass qclass, IPAddress server)
        {
            return Query(name, qtype, qclass, new[] {server});
        }

        /// <summary>
        /// Ask a DNS question to a specific server
        /// </summary>
        public static Response Query(string name, QType qtype, QClass qclass = QClass.IN, IEnumerable<IPAddress> servers = null)
        {
            if (servers == null)
                servers = Network.Dns;

            var request = new Request { new Question(name, qtype, qclass)};

            return Query(request, servers);
        }

        public static Response Query(Request request, IEnumerable<IPAddress> servers)
        {
            var requestByte = request.Write;
            var buffer = new byte[32*1024];

            for (int intAttempts = 0; intAttempts < 3; intAttempts++)
            {
                foreach(var server in servers)
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

                    try
                    {
                        socket.SendTo(requestByte,new IPEndPoint(server,53));
                        int size = socket.Receive(buffer);
                        var rbyte = new byte[size];
                        Array.Copy(buffer, rbyte, size);

                        var resp = Serializer.ReadResponse(rbyte);
                        return resp;
                    }
                    catch (SocketException)
                    {
                        continue; // next try
                    }
                    finally
                    {
                        socket.Close();
                    }
                }
            }


            foreach (var ip in servers)
            {
                var endPoint = new IPEndPoint(ip, 53);

                try
                {
                    var c = new UdpClient { Client = { ReceiveTimeout = 1000, SendTimeout = 1000 } };
                    c.Send(requestByte, requestByte.Length, endPoint);

                    var resp = Serializer.ReadResponse(c.Receive(ref endPoint));
                    if (resp.AllRecords.Length > 0)
                    {
                        return resp;
                    }
                }
                catch (SocketException)
                {
                }
            }
            return new Response();
        }

        /// <summary>
        /// Query A records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> A(string name)
        {
            return Query(name, QType.A).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query AAAA records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> AAAA(string name)
        {
            return Query(name, QType.AAAA).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query CNAME records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> CNAME(string name)
        {
            return Query(name, QType.CNAME).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query MX records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> MX(string name)
        {
            return Query(name, QType.MX).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query NS records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> NS(string name)
        {
            return Query(name, QType.NS).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query PTR records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> PTR(string name)
        {
            return Query(name, QType.PTR).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query SOA records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> SOA(string name)
        {
            return Query(name, QType.SOA).Answers.Select(x => x.ToString());
        }

        /// <summary>
        /// Query TXT records for name into system DNS server
        /// </summary>
        /// <param name="name">domain to query</param>
        /// <returns></returns>
        public static IEnumerable<string> TXT(string name)
        {
            return Query(name, QType.TXT).Answers.Select(x => x.ToString());
        }
    }
}