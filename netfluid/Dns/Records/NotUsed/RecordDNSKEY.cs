/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record DNSKEY (work in progress)
    /// </summary>
        [Serializable]
    public class RecordDNSKEY : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}