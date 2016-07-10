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

using System;
using System.Net;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record A
    /// </summary>
    [Serializable]
    public class RecordA : Record
    {
        /// <summary>
        /// First IPV4 byte
        /// </summary>
        public byte A;

        /// <summary>
        /// Second IPV4 byte
        /// </summary>
        public byte B;

        /// <summary>
        /// Third IPV4 byte
        /// </summary>
        public byte C;

        /// <summary>
        /// Fourth IPV4 byte
        /// </summary>
        public byte D;

        public IPAddress Address()
        {
             return IPAddress.Parse(ToString());
        }

        public void Address(IPAddress value)
        {
            byte[] arr = value.GetAddressBytes();
            A = arr[0];
            B = arr[1];
            C = arr[2];
            D = arr[3];
        }

        public static RecordA Parse(string s)
        {
            var a = new RecordA();
            a.Address(IPAddress.Parse(s));
            return a;
        }

        public static implicit operator RecordA(string s)
        {
            var a = new RecordA();
            a.Address(IPAddress.Parse(s));
            return a;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}.{3}", A, B, C, D);
        }
    }
}