using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Netfluid.Dns
{
    public static class DnsClient
    {
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
            return Query(question.QName, question.QType, question.QClass, new[] { server });
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
            return Query(name, qtype, qclass, new[] { server });
        }

        /// <summary>
        /// Ask a DNS question to a specific server
        /// </summary>
        public static Response Query(string name, QType qtype, QClass qclass = QClass.IN, IEnumerable<IPAddress> servers = null)
        {
            if (servers == null)
                servers = Network.Dns;

            var request = new Request { new Question(name, qtype, qclass) };

            return Query(request, servers);
        }

        public static Response Query(Request request, IEnumerable<IPAddress> servers)
        {
            var requestByte = request.Write;
            var buffer = new byte[32 * 1024];

            for (int intAttempts = 0; intAttempts < 3; intAttempts++)
            {
                foreach (var server in servers)
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

                    try
                    {
                        socket.SendTo(requestByte, new IPEndPoint(server, 53));
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
