/*

 */

using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// DNS record CERT (work in progress)
    /// </summary>
        [Serializable]
    public class RecordCERT : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}