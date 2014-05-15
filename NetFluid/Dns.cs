﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        public static Func<Request, Response> OnRequest;

        /// <summary>
        /// Start local DNS Server
        /// </summary>
        /// <param name="ip"></param>
        public static void StartAcceptRequest(IPAddress ip)
        {
            var endPoint = new IPEndPoint(ip, 53);
            var c = new UdpClient(endPoint);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Console.WriteLine("CYCLE");
                    try
                    {
                        var buffer = c.Receive(ref endPoint);

                        Console.WriteLine("RECIEVED");

                        var req = Serializer.ReadRequest(new MemoryStream(buffer));

                        Console.WriteLine("PARSED");

                        if (OnRequest == null)
                            continue;

                        var resp = OnRequest(req);

                        Console.WriteLine("EXECUTED");

                        var r = Serializer.WriteResponse(resp);

                        Console.WriteLine("SERIALIZED");

                        c.Send(r, r.Length, endPoint);

                        Console.WriteLine("SENT");
                    }
                    catch (Exception exception)
                    {
                        c.Close();
                        Console.WriteLine("EXCEPTION");

                        endPoint = new IPEndPoint(ip, 53);
                        c = new UdpClient(endPoint);
                        Console.WriteLine("REASSIGNED");
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
        public static Record[] Query(Question question, string server)
        {
            return Query(question.QName, question.QType, question.QClass, new[] { IPAddress.Parse(server) });
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


            foreach (var ip in servers)
            {
                var endPoint = new IPEndPoint(ip, 53);

                try
                {
                    var c = new UdpClient();
                    var r = request.Write;
                    c.Send(r, r.Length, endPoint);
                    var data = c.Receive(ref endPoint);
                    var resp = Serializer.ReadResponse(data);
                    if (resp.Answers.Count > 0)
                        return resp.Records;
                }
                catch (SocketException exception)
                {
                    Console.WriteLine(exception);
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