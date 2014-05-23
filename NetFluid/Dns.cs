using System;
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
    ///     Provide methods for DNS query.
    /// </summary>
    public static class Dns
    {
        public static bool AcceptingRequest { get; private set; }

        /// <summary>
        /// Executed when Local DNS Server recieve a request
        /// </summary>
        public static event Func<Request, Response> OnRequest;

        /// <summary>
        /// Start local DNS Server with automatic answers for given domain 
        /// </summary>
        /// <param name="domain"></param>
        public static void WrapDomain(string domain)
        {
            OnRequest += (req) =>
            {
                var r = new Response(req);
                req.ForEach(q =>
                {
                    if (q.QName.EndsWith(domain))
                        switch (q.QType)
                        {
                            case QType.A:
                                    foreach (var ip in Engine.Interfaces.Select(x => x.Endpoint.Address).Where(x => x.AddressFamily == AddressFamily.InterNetworkV6))
                                        r.Answers.Add(new RecordA { Name = q.QName, Address = ip, TimeLived = 0, TTL = 3600 });
                            break;
                            case QType.AAAA:
                                    foreach (var ip in Engine.Interfaces.Select(x => x.Endpoint.Address).Where(x => x.AddressFamily == AddressFamily.InterNetworkV6))
                                        r.Answers.Add(new RecordA { Name = q.QName, Address = ip, TimeLived = 0, TTL = 3600 });
                            break;
                            case QType.CNAME:
                                r.Answers.Add(new RecordCNAME { Name = domain, Alias = q.QName, TimeLived = 0, TTL = 3600 });
                            break;
                            case QType.MX:
                                r.Answers.Add(new RecordMX{Exchange = domain});
                            break;
                        }
                });
                return r;
            };
            StartAcceptRequest(IPAddress.Any);
        }

        /// <summary>
        /// Start local DNS Server giving A and AAAA records for loaded application
        /// </summary>
        public static void AutoWrap()
        {
            OnRequest += (req) =>
            {
                var r = new Response(req);
                req.ForEach(q =>
                {
                    switch (q.QType)
                    {
                        case QType.A:
                            if (Engine.Hostnames.Contains(q.QName))
                                foreach (var ip in Engine.Interfaces.Select(x=>x.Endpoint.Address).Where(x=>x.AddressFamily == AddressFamily.InterNetworkV6))
                                {
                                    r.Answers.Add(new RecordA { Name = q.QName, Address = ip, TimeLived = 0, TTL = 3600 });
                                }
                        break;
                        case QType.AAAA:
                        if (Engine.Hostnames.Contains(q.QName))
                            foreach (var ip in Engine.Interfaces.Select(x => x.Endpoint.Address).Where(x => x.AddressFamily == AddressFamily.InterNetworkV6))
                            {
                                r.Answers.Add(new RecordA { Name = q.QName, Address = ip, TimeLived = 0, TTL = 3600 });
                            }
                        break;
                    }
                });
                return r;
            };
            StartAcceptRequest(IPAddress.Any);
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
                    catch (Exception)
                    {
                        c.Close();
                        endPoint = new IPEndPoint(ip, 53);
                        c = new UdpClient(endPoint);
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
        public static Record[] Query(Question question)
        {
            return Query(question.QName, question.QType, question.QClass, AcceptingRequest ? Network.Dns.Where(x=>!x.IsLocal()) : Network.Dns);
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
        public static Record[] Query(string name, QType qtype, QClass qclass = QClass.IN, IEnumerable<IPAddress> servers = null)
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
                    var c = new UdpClient {Client = {ReceiveTimeout = 500, SendTimeout = 500}};

                    var r = request.Write;
                    c.Send(r, r.Length, endPoint);

                    var resp = Serializer.ReadResponse(c.Receive(ref endPoint));
                    if (resp.Answers.Count > 0)
                        return resp.Records;
                }
                catch (SocketException)
                {
                    ////Console.WriteLine(exception);
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