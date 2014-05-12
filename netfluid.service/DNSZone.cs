using System.Collections.Generic;
using NetFluid.DNS.Records;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    class DNSZone
    {
        public List<RecordA> A;
        public List<RecordMX> MX;
        public List<RecordTXT> TXT;
        public List<RecordCNAME> CNAME;
    }
}
