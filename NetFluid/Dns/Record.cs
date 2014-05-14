// Stuff records are made of

using System;
using NetFluid.DNS.Records;

namespace NetFluid.DNS
{
    [Serializable]
    public abstract class Record: IDatabaseObject
    {
        /// <summary>
        /// Database id to store and handle record
        /// </summary>
        public string Id { get; set; }
   
        /// <summary>
        ///     Specifies type class of resource record, mostly IN but can be CS, CH or HS
        /// </summary>
        public Class Class;

        /// <summary>
        /// The name of the node to which this resource record pertains
        /// </summary>
        public string Name;

        /// <summary>
        /// Time lived
        /// </summary>
        public int TimeLived;

        protected Record()
        {
            TimeLived = 0;
            Name = "netfluid.org";
            Class = Class.IN;
            TTL = int.MaxValue - 1;
        }

        /// <summary>
        ///     Time to live, the time interval that the resource record may be cached
        /// </summary>
        public uint TTL { get; set; }

        /// <summary>
        /// Return .net type of record from enum RecordType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type Type(RecordType type)
        {
            switch (type)
            {
                case RecordType.A:
                    return typeof(RecordA);
                case RecordType.NS:
                    return typeof(RecordNS);
                case RecordType.MD:
                    return typeof(RecordMD);
                case RecordType.MF:
                    return typeof(RecordMF);
                case RecordType.CNAME:
                    return typeof(RecordCNAME);
                case RecordType.SOA:
                    return typeof(RecordSOA);
                case RecordType.MB:
                    return typeof(RecordMB);
                case RecordType.MG:
                    return typeof(RecordMG);
                case RecordType.MR:
                    return typeof(RecordMR);
                case RecordType.NULL:
                    return typeof(RecordNULL);
                case RecordType.WKS:
                    return typeof(RecordWKS);
                case RecordType.PTR:
                    return typeof(RecordPTR);
                case RecordType.HINFO:
                    return typeof(RecordHINFO);
                case RecordType.MINFO:
                    return typeof(RecordMINFO);
                case RecordType.MX:
                    return typeof(RecordMX);
                case RecordType.TXT:
                    return typeof(RecordTXT);
                case RecordType.RP:
                    return typeof(RecordRP);
                case RecordType.AFSDB:
                    return typeof(RecordAFSDB);
                case RecordType.X25:
                    return typeof(RecordX25);
                case RecordType.ISDN:
                    return typeof(RecordISDN);
                case RecordType.RT:
                    return typeof(RecordRT);
                case RecordType.NSAP:
                    return typeof(RecordNSAP);
                case RecordType.NSAPPTR:
                    return typeof(RecordNSAPPTR);
                case RecordType.SIG:
                    return typeof(RecordSIG);
                case RecordType.KEY:
                    return typeof(RecordKEY);
                case RecordType.PX:
                    return typeof(RecordPX);
                case RecordType.GPOS:
                    return typeof(RecordGPOS);
                case RecordType.AAAA:
                    return typeof(RecordAAAA);
                case RecordType.LOC:
                    return typeof(RecordLOC);
                case RecordType.NXT:
                    return typeof(RecordNXT);
                case RecordType.EID:
                    return typeof(RecordEID);
                case RecordType.NIMLOC:
                    return typeof(RecordNIMLOC);
                case RecordType.SRV:
                    return typeof(RecordSRV);
                case RecordType.ATMA:
                    return typeof(RecordATMA);
                case RecordType.NAPTR:
                    return typeof(RecordNAPTR);
                case RecordType.KX:
                    return typeof(RecordKX);
                case RecordType.CERT:
                    return typeof(RecordCERT);
                case RecordType.A6:
                    return typeof(RecordA6);
                case RecordType.DNAME:
                    return typeof(RecordDNAME);
                case RecordType.SINK:
                    return typeof(RecordSINK);
                case RecordType.OPT:
                    return typeof(RecordOPT);
                case RecordType.APL:
                    return typeof(RecordAPL);
                case RecordType.DS:
                    return typeof(RecordDS);
                case RecordType.SSHFP:
                    return typeof(RecordSSHFP);
                case RecordType.IPSECKEY:
                    return typeof(RecordIPSECKEY);
                case RecordType.RRSIG:
                    return typeof(RecordRRSIG);
                case RecordType.NSEC:
                    return typeof(RecordNSEC);
                case RecordType.DNSKEY:
                    return typeof(RecordDNSKEY);
                case RecordType.DHCID:
                    return typeof(RecordDHCID);
                case RecordType.NSEC3:
                    return typeof(RecordNSEC3);
                case RecordType.NSEC3PARAM:
                    return typeof(RecordNSEC3PARAM);
                case RecordType.HIP:
                    return typeof(RecordHIP);
                case RecordType.SPF:
                    return typeof(RecordSPF);
                case RecordType.UINFO:
                    return typeof(RecordUINFO);
                case RecordType.UID:
                    return typeof(RecordUID);
                case RecordType.GID:
                    return typeof(RecordGID);
                case RecordType.UNSPEC:
                    return typeof(RecordUNSPEC);
                case RecordType.TKEY:
                    return typeof(RecordTKEY);
                case RecordType.TSIG:
                    return typeof(RecordTSIG);
                default:
                    return typeof(RecordUnknown);
            }
        }

        public static Record FromType(RecordType type)
        {
            switch (type)
            {
                case RecordType.A:
                    return new RecordA();
                case RecordType.NS:
                    return new RecordNS();
                case RecordType.MD:
                    return new RecordMD();
                case RecordType.MF:
                    return new RecordMF();
                case RecordType.CNAME:
                    return new RecordCNAME();
                case RecordType.SOA:
                    return new RecordSOA();
                case RecordType.MB:
                    return new RecordMB();
                case RecordType.MG:
                    return new RecordMG();
                case RecordType.MR:
                    return new RecordMR();
                case RecordType.NULL:
                    return new RecordNULL();
                case RecordType.WKS:
                    return new RecordWKS();
                case RecordType.PTR:
                    return new RecordPTR();
                case RecordType.HINFO:
                    return new RecordHINFO();
                case RecordType.MINFO:
                    return new RecordMINFO();
                case RecordType.MX:
                    return new RecordMX();
                case RecordType.TXT:
                    return new RecordTXT();
                case RecordType.RP:
                    return new RecordRP();
                case RecordType.AFSDB:
                    return new RecordAFSDB();
                case RecordType.X25:
                    return new RecordX25();
                case RecordType.ISDN:
                    return new RecordISDN();
                case RecordType.RT:
                    return new RecordRT();
                case RecordType.NSAP:
                    return new RecordNSAP();
                case RecordType.NSAPPTR:
                    return new RecordNSAPPTR();
                case RecordType.SIG:
                    return new RecordSIG();
                case RecordType.KEY:
                    return new RecordKEY();
                case RecordType.PX:
                    return new RecordPX();
                case RecordType.GPOS:
                    return new RecordGPOS();
                case RecordType.AAAA:
                    return new RecordAAAA();
                case RecordType.LOC:
                    return new RecordLOC();
                case RecordType.NXT:
                    return new RecordNXT();
                case RecordType.EID:
                    return new RecordEID();
                case RecordType.NIMLOC:
                    return new RecordNIMLOC();
                case RecordType.SRV:
                    return new RecordSRV();
                case RecordType.ATMA:
                    return new RecordATMA();
                case RecordType.NAPTR:
                    return new RecordNAPTR();
                case RecordType.KX:
                    return new RecordKX();
                case RecordType.CERT:
                    return new RecordCERT();
                case RecordType.A6:
                    return new RecordA6();
                case RecordType.DNAME:
                    return new RecordDNAME();
                case RecordType.SINK:
                    return new RecordSINK();
                case RecordType.OPT:
                    return new RecordOPT();
                case RecordType.APL:
                    return new RecordAPL();
                case RecordType.DS:
                    return new RecordDS();
                case RecordType.SSHFP:
                    return new RecordSSHFP();
                case RecordType.IPSECKEY:
                    return new RecordIPSECKEY();
                case RecordType.RRSIG:
                    return new RecordRRSIG();
                case RecordType.NSEC:
                    return new RecordNSEC();
                case RecordType.DNSKEY:
                    return new RecordDNSKEY();
                case RecordType.DHCID:
                    return new RecordDHCID();
                case RecordType.NSEC3:
                    return new RecordNSEC3();
                case RecordType.NSEC3PARAM:
                    return new RecordNSEC3PARAM();
                case RecordType.HIP:
                    return new RecordHIP();
                case RecordType.SPF:
                    return new RecordSPF();
                case RecordType.UINFO:
                    return new RecordUINFO();
                case RecordType.UID:
                    return new RecordUID();
                case RecordType.GID:
                    return new RecordGID();
                case RecordType.UNSPEC:
                    return new RecordUNSPEC();
                case RecordType.TKEY:
                    return new RecordTKEY();
                case RecordType.TSIG:
                    return new RecordTSIG();
                default:
                    return new RecordUnknown();
            }
        }
    }
}