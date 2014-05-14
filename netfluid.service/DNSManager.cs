using System.Net;
using MongoDB.Bson;
using NetFluid.DNS;
using NetFluid.DNS.Records;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    public class A: RecordA,MongoObject
    {
        public string Id { get; set; }
    }

    public class AAAA : RecordAAAA, MongoObject
    {
        public string Id { get; set; }
    }

    public class CNAME : RecordCNAME, MongoObject
    {
        public string Id { get; set; }
    }

    public class MX : RecordMX, MongoObject
    {
        public string Id { get; set; }
    }

    public class TXT : RecordTXT, MongoObject
    {
        public string Id { get; set; }
    }

    [Route("dns")]
    public class DNSManager:FluidPage
    {
        public static Repository<A> A;

        public static Repository<AAAA> AAAA;

        public static Repository<CNAME> CNAME;

        public static Repository<MX> MX;

        public static Repository<TXT> TXT;

        static DNSManager()
        {
            A = new Repository<A>("mongodb://localhost", "NetFluidService");
            AAAA = new Repository<AAAA>("mongodb://localhost", "NetFluidService");
            CNAME = new Repository<CNAME>("mongodb://localhost", "NetFluidService");
            MX = new Repository<MX>("mongodb://localhost", "NetFluidService");
            TXT = new Repository<TXT>("mongodb://localhost", "NetFluidService");

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
