/*
 3.4.1. A RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ADDRESS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

ADDRESS         A 32 bit Internet address.

Hosts that have multiple Internet addresses will have multiple A
records.
 * 
 */

using System.Net;

namespace NetFluid.DNS.Records
{
    public class RecordA : Record
    {
        public byte A;
        public byte B;
        public byte C;
        public byte D;

        public IPAddress Address
        {
            get { return IPAddress.Parse(ToString()); }
            set
            {
                byte[] arr = value.GetAddressBytes();
                A = arr[0];
                B = arr[1];
                C = arr[2];
                D = arr[3];
            }
        }

        public static implicit operator RecordA(string s)
        {
            return new RecordA {Address = IPAddress.Parse(s)};
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", A, B, C, D);
        }
    }
}