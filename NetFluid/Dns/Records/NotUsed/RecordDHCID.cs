/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record DHCID (work in progress)
    /// </summary>
        [Serializable]
    public class RecordDHCID : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}