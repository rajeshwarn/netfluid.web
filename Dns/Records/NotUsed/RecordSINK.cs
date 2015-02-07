/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record SINK (work in progress)
    /// </summary>
            [Serializable]
    public class RecordSINK : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}