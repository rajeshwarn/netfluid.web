/*

 */

using System;

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class RecordNSEC3 : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}