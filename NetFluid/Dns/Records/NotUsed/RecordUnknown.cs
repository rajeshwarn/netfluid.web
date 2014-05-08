using System;

namespace NetFluid.DNS.Records
{
    [Serializable]
    public class RecordUnknown : Record
    {
        public byte[] RDATA;
    }
}