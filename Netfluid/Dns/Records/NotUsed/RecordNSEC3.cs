/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record NSEC3 (work in progress)
    /// </summary>
        [Serializable]
    public class RecordNSEC3 : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}