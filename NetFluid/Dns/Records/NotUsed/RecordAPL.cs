/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record APL (work in progress)
    /// </summary>
        [Serializable]
    public class RecordAPL : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}