using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NetFluid.DNS;
using NetFluid.DNS.Records;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("dns")]
    public class DNSManager:FluidPage
    {
        public static Repository<Record> store;

        public static IEnumerable<RecordA> A 
        {
            get { return store.OfType<RecordA>(); }
        }

        public static IEnumerable<RecordAAAA> AAAA
        {
            get { return store.OfType<RecordAAAA>(); }
        }

        public static IEnumerable<RecordCNAME> CNAME
        {
            get { return store.OfType<RecordCNAME>(); }
        }

        public static IEnumerable<RecordMX> MX
        {
            get { return store.OfType<RecordMX>(); }
        }

        public static IEnumerable<RecordTXT> TXT
        {
            get { return store.OfType<RecordTXT>(); }
        }

        static DNSManager()
        {
            store = new Repository<Record>("mongodb://localhost", "NetFluidService");
            Dns.StartAcceptRequest(IPAddress.Any);
            Dns.StartAcceptRequest(IPAddress.IPv6Any);

            Dns.OnRequest += Dns_OnRequest;
        }

        static Response Dns_OnRequest(Request request)
        {
            var response = new Response();

            foreach (var question in request)
            {
                if (question.QType >= QType.IXFR && question.QType <= QType.ANY)
                {
                    //NOT IMPLEMENTED
                    continue;
                }
                var name = question.QName;
                var type = question.QType;
                var found = store.Where(x => x.Name == name).ToArray().Where(x=>x.RecordType == (RecordType)type);

                if (found.Any())
                {
                    response.Answers.AddRange(found);
                }
                else
                {
                    Task.Factory.StartNew(() =>
                    {
                        //We don't have the response, foward to google and add to database
                        var fow = Dns.Query(question.QName, question.QType, question.QClass, IPAddress.Parse("8.8.8.8"));
                        store.Save(fow);
                        response.Answers.AddRange(fow);
                    });
                }
            }
            return response;
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update(string type)
        {
            store.Save(Request.Values.ToObject(Record.Type(type)) as Record);
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string type, string id)
        {
            store.Remove(id);
            return new RedirectResponse("/");
        }
    }
}
