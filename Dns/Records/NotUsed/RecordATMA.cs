/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record ATMA (work in progress)
    /// </summary>
        [Serializable]
    public class RecordATMA : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}