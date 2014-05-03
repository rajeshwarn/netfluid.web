/*

 */

namespace NetFluid.DNS.Records
{
    public class RecordOPT : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}