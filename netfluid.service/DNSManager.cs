using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NetFluid.DNS;
using NetFluid.DNS.Records;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    public class A: RecordA,MongoObject
    {
        public ObjectId _id { get; set; }
    }

    public class AAAA : RecordAAAA, MongoObject
    {
        public ObjectId _id { get; set; }
    }

    public class CNAME : RecordCNAME, MongoObject
    {
        public ObjectId _id { get; set; }
    }

    public class MX : RecordMX, MongoObject
    {
        public ObjectId _id { get; set; }
    }

    public class TXT : RecordTXT, MongoObject
    {
        public ObjectId _id { get; set; }
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
                case "Alias":
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
        public IResponse Delete(string id)
        {
            return new RedirectResponse("/");
        }
    }
}
