using Heijden.DNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFluid
{
    //DNS INTEGRATION, QUERY AND RECORDS FROM Heijden.DNS http://www.codeproject.com/Articles/23673/DNS-NET-Resolver-C
    
    /// <summary>
    /// Provide methods for DNS query. Server cooming soon
    /// </summary>
    public static class Dns
    {
        static readonly Resolver resolver;

        static Dns()
        {
            resolver = new Resolver();
            resolver.Recursion = true;
            resolver.UseCache = true;
            resolver.DnsServer = "8.8.8.8"; // Google public DNS

            resolver.TimeOut = 1000;
            resolver.Retries = 3;
            resolver.TransportType = TransportType.Udp;
        }

        public static IEnumerable<string> A(string name)
        {
            Response response = resolver.Query(name, QType.A, QClass.IN);
            return response.RecordsA.Select(x => x.ToString());
        }

        public static IEnumerable<string> AAAA(string name)
        {
            Response response = resolver.Query(name, QType.AAAA, QClass.IN);
            return response.RecordsAAAA.Select(x => x.ToString());
        }

        public static IEnumerable<string> CNAME(string name)
        {
            Response response = resolver.Query(name, QType.CNAME, QClass.IN);
            return response.RecordsCNAME.Select(x => x.ToString());
        }

        public static IEnumerable<string> MX(string name)
        {
            Response response = resolver.Query(name, QType.MX, QClass.IN);
            return response.RecordsMX.OrderBy(x=>x.PREFERENCE).Select(x => x.EXCHANGE);
        }

        public static IEnumerable<string> NS(string name)
        {
            Response response = resolver.Query(name, QType.NS, QClass.IN);
            return response.RecordsNS.Select(x => x.ToString());
        }

        public static IEnumerable<string> PTR(string name)
        {
            Response response = resolver.Query(name, QType.PTR, QClass.IN);
            return response.RecordsPTR.Select(x => x.ToString());
        }

        public static IEnumerable<string> SOA(string name)
        {
            Response response = resolver.Query(name, QType.SOA, QClass.IN);
            return response.RecordsSOA.Select(x => x.ToString());
        }

        public static IEnumerable<string> TXT(string name)
        {
            Response response = resolver.Query(name, QType.TXT, QClass.IN);
            return response.RecordsTXT.Select(x => x.ToString());
        }
    }
}
