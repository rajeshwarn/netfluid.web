/*

 */

using System;

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class RecordHIP : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}