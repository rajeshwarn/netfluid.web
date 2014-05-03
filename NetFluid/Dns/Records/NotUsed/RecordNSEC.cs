/*

 */

namespace NetFluid.DNS.Records
{
    public class RecordNSEC : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}