/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record EID (work in progress)
    /// </summary>
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