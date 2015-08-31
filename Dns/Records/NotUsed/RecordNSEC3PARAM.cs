
/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record NSEC3PARAM (work in progress)
    /// </summary>
        [Serializable]
    public class RecordNSEC3PARAM : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}