/*

 */

using System;

namespace Netfluid.DNS.Records
{
    /// <summary>
    /// DNS record HIP (work in progress)
    /// </summary>
        [Serializable]
    public class RecordHIP : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}