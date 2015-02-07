/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record SPF (work in progress)
    /// </summary>

    [Serializable]
    public class RecordSPF : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}