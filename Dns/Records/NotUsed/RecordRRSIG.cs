/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record PRSIG (work in progress)
    /// </summary>
        [Serializable]
    public class RecordRRSIG : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}