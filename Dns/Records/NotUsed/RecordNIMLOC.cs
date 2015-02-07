/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record NIMLOC (work in progress)
    /// </summary>
        [Serializable]
    public class RecordNIMLOC : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}