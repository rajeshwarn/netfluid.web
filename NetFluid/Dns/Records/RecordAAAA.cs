#region Rfc info

/*
2.2 AAAA data format

   A 128 bit IPv6 address is encoded in the data portion of an AAAA
   resource record in network byte order (high-order byte first).
 */

#endregion

using System;
using System.Net;

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class RecordAAAA : Record
    {
        public ushort A;
        public ushort B;
        public ushort C;
        public ushort D;
        public ushort E;
        public ushort F;
        public ushort G;
        public ushort H;


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

        public static implicit operator RecordAAAA(string s)
        {
            return new RecordAAAA {Address = IPAddress.Parse(s)};
        }

        public override string ToString()
        {
            return string.Format("{0:x}:{1:x}:{2:x}:{3:x}:{4:x}:{5:x}:{6:x}:{7:x}", A, B, C, D, E, F, G, H);
        }
    }
}