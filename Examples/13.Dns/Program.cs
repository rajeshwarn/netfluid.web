using System;
using System.Linq;
using System.Net;
using NetFluid;
using NetFluid.DNS;
using NetFluid.DNS.Records;


namespace _13.Dns
{
    class Program
    {
        static void Main(string[] args)
        {

            NetFluid.Dns.MX("netfluid.org").ForEach(Console.WriteLine);

            foreach (QType s in Enum.GetValues(typeof(RecordType)))
            {
                var r = NetFluid.Dns.Query("microsoft.com", s);
                if (r.Any())
                {
                    Console.WriteLine(s);
                    foreach (var record in r)
                    {
                        Console.WriteLine(record);
                    }
                    Console.WriteLine("");
                }
            }
            Console.WriteLine("FINITO");
            Console.ReadLine();
        }

        static NetFluid.DNS.Records.Response con_OnRequest(NetFluid.DNS.Records.Request arg)
        {
            var rec = new RecordA();
            var k = new Response();
            k.Answers.Add(rec);
            return k;
        }
    }
}
