/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record OPT (work in progress)
    /// </summary>
        [Serializable]
    public class RecordOPT : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}