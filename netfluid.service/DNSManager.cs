using System.Collections.Generic;
using System.Linq;
using NetFluid.DNS;
using NetFluid.DNS.Records;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("dns")]
    public class DNSManager:FluidPage
    {
        public static Repository<Record> Records { get; private set; }

        public static IEnumerable<RecordA> A { get { return Records.OfType<RecordA>(); } }

        public static IEnumerable<RecordAAAA> AAAA { get { return Records.OfType<RecordAAAA>(); } }

        public static IEnumerable<RecordCNAME> CNAME { get { return Records.OfType<RecordCNAME>(); } }

        public static IEnumerable<RecordMX> MX { get { return Records.OfType<RecordMX>(); } }

        public static IEnumerable<RecordTXT> TXT { get { return Records.OfType<RecordTXT>(); } }

        static DNSManager()
        {
            Records = new Repository<Record>("mongodb://localhost", "NetFluidService");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update(string type)
        {
            Record record=null;

            switch (type)
            {
                case "A":
                    record = Request.Values.ToObject<RecordA>();
                break;
                case "AAAA":
                    record = Request.Values.ToObject<RecordAAAA>();
                break;
                case "CNAME":
                    record = Request.Values.ToObject<RecordCNAME>();
                break;
                case "MX":
                    record = Request.Values.ToObject<RecordMX>();
                break;
                case "TXT":
                    record = Request.Values.ToObject<RecordTXT>();
                break;
            }

            Records.Save(record);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            return new RedirectResponse("/");
        }
    }
}
