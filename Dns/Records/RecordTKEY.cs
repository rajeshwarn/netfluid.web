using System;

/*
 * http://tools.ietf.org/rfc/rfc2930.txt
 * 
2. The TKEY Resource Record

   The TKEY resource record (RR) has the structure given below.  Its RR
   type code is 249.

      Field       RecordType         Comment
      -----       ----         -------
       Algorithm:   domain
       Inception:   u_int32_t
       Expiration:  u_int32_t
       Mode:        u_int16_t
       Error:       u_int16_t
       Key Size:    u_int16_t
       Key Write:    octet-stream
       Other Size:  u_int16_t
       Other Write:  octet-stream  undefined by this specification

 */

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record TKEY
    /// </summary>
        [Serializable]
    public class RecordTKEY : Record
    {
        [DomainName] public string ALGORITHM;
        public UInt16 ERROR;
        public UInt32 EXPIRATION;
        public UInt32 INCEPTION;
        public byte[] KEYDATA;
        public UInt16 KEYSIZE;
        public UInt16 MODE;
        public byte[] OTHERDATA;
        public UInt16 OTHERSIZE;

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4}",
                ALGORITHM,
                INCEPTION,
                EXPIRATION,
                MODE,
                ERROR);
        }
    }
}