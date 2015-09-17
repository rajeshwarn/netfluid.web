using System;

#region Rfc info

/*
 * http://www.ietf.org/rfc/rfc2535.txt
 * 4.1 SIG RDATA Format

   The RDATA portion of a SIG RR is as shown below.  The integrity of
   the RDATA information is protected by the signature field.

                           1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |        type covered           |  algorithm    |     labels    |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                         original TTL                          |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                      signature expiration                     |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                      signature inception                      |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |            key  tag           |                               |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+         signer's name         +
      |                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-/
      /                                                               /
      /                            signature                          /
      /                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+


*/

#endregion

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record SIG
    /// </summary>
        [Serializable]
    public class RecordSIG : Record
    {
        public byte ALGORITHM;
        public UInt16 KEYTAG;
        public byte LABELS;
        public UInt32 ORIGINALTTL;
        public string SIGNATURE;
        public UInt32 SIGNATUREEXPIRATION;
        public UInt32 SIGNATUREINCEPTION;

        [DomainName] public string SIGNERSNAME;
        public UInt16 TYPECOVERED;

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} \"{8}\"",
                TYPECOVERED,
                ALGORITHM,
                LABELS,
                ORIGINALTTL,
                SIGNATUREEXPIRATION,
                SIGNATUREINCEPTION,
                KEYTAG,
                SIGNERSNAME,
                SIGNATURE);
        }
    }
}