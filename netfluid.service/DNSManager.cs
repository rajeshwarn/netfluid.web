using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            foreach (var question in request)
            {
                switch (question.QType)
                {
                    case QType.A:

                    break;
                    case QType.AAAA:
                    case QType.CNAME:
                    case QType.MX:
                    case QType.TXT:
                    break;
                }
            }
            return null;
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update(string type)
        {
            Record record;
            switch (type)
            {
                case "A":
                    A.Save(Request.Values.ToObject<A>());
                break;
                case "AAAA":
                    AAAA.Save(Request.Values.ToObject<AAAA>());
                break;
                case "CNAME":
                    CNAME.Save(Request.Values.ToObject<CNAME>());
                break;
                case "MX":
                    MX.Save(Request.Values.ToObject<MX>());
                break;
                case "TXT":
                    TXT.Save(Request.Values.ToObject<TXT>());
                break;
            }
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string type, string id)
        {
            switch (type)
            {
                case "A":
                    A.Remove(id);
                    return new RedirectResponse("/#dns-a");
                case "AAAA":
                    AAAA.Remove(id);
                    return new RedirectResponse("/#dns-aaaa");
                case "CNAME":
                    CNAME.Remove(id);
                    return new RedirectResponse("/#dns-cname");
                case "MX":
                    MX.Remove(id);
                    return new RedirectResponse("/#dns-mx");
                case "TXT":
                    TXT.Remove(id);
                    return new RedirectResponse("/#dns-txt");
            }
            return new RedirectResponse("/");
        }
    }
}
