

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record A6 (work in progress)
    /// </summary>
        [Serializable]
    public class RecordA6 : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}