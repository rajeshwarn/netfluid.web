using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFluid.DNS;
using NetFluid.DNS.Records;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("dns")]
    class DnsManager:FluidPage
    {
        private static readonly Repository<Record> records;
 
        static DnsManager()
        {
            records = new Repository<Record>("mongodb://localhost", "NetFluidService");
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

            records.Save(record);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            return new RedirectResponse("/");
        }
    }
}
