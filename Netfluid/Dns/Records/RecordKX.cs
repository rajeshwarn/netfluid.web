using System;

/*
 * http://tools.ietf.org/rfc/rfc2230.txt
 * 
 * 3.1 KX RDATA format

   The KX DNS record has the following RDATA format:

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                  PREFERENCE                   |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   EXCHANGER                   /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

   where:

   PREFERENCE      A 16 bit non-negative integer which specifies the
                   preference given to this RR among other KX records
                   at the same owner.  Lower values are preferred.

   EXCHANGER       A <domain-name> which specifies a host willing to
                   act as a mail exchange for the owner name.

   KX records MUST cause type A additional section processing for the
   host specified by EXCHANGER.  In the event that the host processing
   the DNS transaction supports IPv6, KX records MUST also cause type
   AAAA additional section processing.

   The KX RDATA field MUST NOT be compressed.

 */

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record KX
    /// </summary>
        [Serializable]
    public class RecordKX : Record, IComparable
    {
        [DomainName] public string Exchanger;
        public ushort Preference;


        public int CompareTo(object objA)
        {
            var recordKX = objA as RecordKX;
            if (recordKX == null)
                return -1;
            if (Preference > recordKX.Preference)
                return 1;
            if (Preference < recordKX.Preference)
                return -1;
            return String.Compare(Exchanger, recordKX.Exchanger, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Preference, Exchanger);
        }
    }
}