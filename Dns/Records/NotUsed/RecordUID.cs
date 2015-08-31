/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record UID (work in progress)
    /// </summary>
    [Serializable]
    public class RecordUID : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}