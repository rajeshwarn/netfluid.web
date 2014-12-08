using System;

namespace NetFluid.DNS.Records
{
    /// <summary>
    /// Undetected / unsupported DNS record type
    /// </summary>
    [Serializable]
    public class RecordUnknown : Record
    {
        public byte[] RDATA;
    }
}