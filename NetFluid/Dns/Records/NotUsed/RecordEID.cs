/*

 */

using System;

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class RecordEID : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}