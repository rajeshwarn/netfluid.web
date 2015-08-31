/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record GID (work in progress)
    /// </summary>
        [Serializable]
    public class RecordGID : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}