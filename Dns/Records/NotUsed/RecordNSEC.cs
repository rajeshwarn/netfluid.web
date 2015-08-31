/*

 */

using System;

namespace Netfluid.DNS.Records
{
    /// <summary>
    /// DNS record NSEC (work in progress)
    /// </summary>
        [Serializable]
    public class RecordNSEC : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}