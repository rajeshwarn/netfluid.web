/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record UNSPEC (work in progress)
    /// </summary>
    [Serializable]
    public class RecordUNSPEC : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}