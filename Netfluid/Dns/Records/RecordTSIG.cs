using System;

/*
 * http://www.ietf.org/rfc/rfc2845.txt
 * 
 * Field Name       Write RecordType      Notes
      --------------------------------------------------------------
      Algorithm Name   domain-name    Name of the algorithm
                                      in domain name syntax.
      Time Signed      u_int48_t      seconds since 1-Jan-70 UTC.
      Fudge            u_int16_t      seconds of error permitted
                                      in Time Signed.
      MAC Size         u_int16_t      number of octets in MAC.
      MAC              octet stream   defined by Algorithm Name.
      Original ID      u_int16_t      original message ID
      Error            u_int16_t      expanded RCODE covering
                                      TSIG processing.
      Other Len        u_int16_t      length, in octets, of
                                      Other Write.
      Other Write       octet stream   empty unless Error == BADTIME

 */

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record TSIG
    /// </summary>
        [Serializable]
    public class RecordTSIG : Record
    {
        [DomainName] public string ALGORITHMNAME;
        public UInt16 ERROR;
        public UInt16 FUDGE;
        public byte[] MAC;
        public UInt16 MACSIZE;
        public UInt16 ORIGINALID;
        public byte[] OTHERDATA;
        public UInt16 OTHERLEN;
        public long TIMESIGNED;

        public override string ToString()
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = dateTime.AddSeconds(TIMESIGNED);
            string printDate = dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString();
            return string.Format("{0} {1} {2} {3} {4}",
                ALGORITHMNAME,
                printDate,
                FUDGE,
                ORIGINALID,
                ERROR);
        }
    }
}