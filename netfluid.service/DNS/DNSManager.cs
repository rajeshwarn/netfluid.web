using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetFluid.Collections;
using NetFluid.DNS;
using NetFluid.DNS.Records;

namespace NetFluid.Service
{
    [Route("dns")]
    public class DNSManager:FluidPage
    {
        public override void OnLoad()
        {
            A = new XMLRepository<RecordA>("dns.a.xml");
            AAAA = new XMLRepository<RecordAAAA>("dns.aaaa.xml");
            CNAME = new XMLRepository<RecordCNAME>("dns.cname.xml");
            MX = new XMLRepository<RecordMX>("dns.mx.xml");
            TXT = new XMLRepository<RecordTXT>("dns.txt.xml");

            Dns.AutoWrap();
            Dns.StartAcceptRequest(IPAddress.Any);
            Dns.OnRequest += Dns_OnRequest;
        }


        public static XMLRepository<RecordA> A;

        public static XMLRepository<RecordAAAA> AAAA;

        public static XMLRepository<RecordCNAME> CNAME;

        public static XMLRepository<RecordMX> MX;

        public static XMLRepository<RecordTXT> TXT;


        static Response Dns_OnRequest(Request request)
        {
            var response = new Response(request);

            foreach (var question in request)
            {
                var name = question.QName;
                IEnumerable<Record> found=null;

                switch (question.QType)
                {
                    case QType.A:
                        found = A.Where(x => x.Name == name); break;
                    case QType.AAAA:
                        found = AAAA.Where(x => x.Name == name); break;
                    case QType.CNAME:
                        found = CNAME.Where(x => x.Name == name); break;
                    case QType.MX:
                        found = MX.Where(x => x.Name == name); break;
                    case QType.TXT:
                        found = TXT.Where(x => x.Name == name); break;
                }

                if (found==null || !found.Any() || question.QType >= QType.IXFR && question.QType <= QType.ANY)
                {
                    var fow = Dns.Query(question,IPAddress.Parse("8.8.8.8"));
                    response.Answers.AddRange(fow);
                }
                else
                {
                    response.Answers.AddRange(found);
                }
            }
            return response;
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update(string type)
        {
            switch (type)
            {
                case "A":
                    A.Save(Request.Values.ToObject(Record.Type(type)) as RecordA); break;
                case "AAAA":
                    AAAA.Save(Request.Values.ToObject(Record.Type(type)) as RecordAAAA); break;
                case "CNAME":
                    CNAME.Save(Request.Values.ToObject(Record.Type(type)) as RecordCNAME); break;
                case "MX":
                    MX.Save(Request.Values.ToObject(Record.Type(type)) as RecordMX); break;
                case "TXT":
                    TXT.Save(Request.Values.ToObject(Record.Type(type)) as RecordTXT); break;
            }
            
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string type, string id)
        {
            switch (type)
            {
                case "A":
                    A.Remove(id); break;
                case "AAAA":
                    AAAA.Remove(id); break;
                case "CNAME":
                    CNAME.Remove(id); break;
                case "MX":
                    MX.Remove(id); break;
                case "TXT":
                    TXT.Remove(id); break;
            }
            return new RedirectResponse("/");
        }
    }
}
