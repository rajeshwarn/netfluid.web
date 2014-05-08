/*

 */

using System;

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class RecordCERT : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}