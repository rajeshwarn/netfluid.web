

using System;

namespace Netfluid.Dns.Records
{
    //TESTME

    /// <summary>
    /// DNS record A6 (work in progress)
    /// </summary>
    [Serializable]
    public class RecordA6 : Record
    {
        public byte[] Address;

        public byte PrefixSize;

        [DomainName]
        public string Dns;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}