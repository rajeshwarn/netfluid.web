/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record IPSECKEY (work in progress)
    /// </summary>
        [Serializable]
    public class RecordIPSECKEY : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}