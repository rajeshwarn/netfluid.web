/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record UINFO (work in progress)
    /// </summary>
    [Serializable]
    public class RecordUINFO : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}