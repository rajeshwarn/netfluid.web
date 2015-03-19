using System;
using System.Collections.Generic;
using NetFluid.DNS.Records;

namespace NetFluid.DNS
{
    /// <summary>
    /// List of DNS query used by DNS server and resolver
    /// </summary>
    [Serializable]
    public class Request : List<Question>
    {
        /// <summary>
        /// Header
        /// </summary>
        public Header Header;

        public Request()
        {
            Header = new Header {OPCODE = OPCode.Query, QDCOUNT = 0, RD = true, ID = (ushort) DateTime.Now.Millisecond};
        }

        /// <summary>
        /// TOBEREMOVED
        /// </summary>
        internal byte[] Write
        {
            get
            {
                var data = new List<byte>();
                Header.QDCOUNT = (ushort) Count;
                data.AddRange(Header.Data);

                foreach (Question q in this)
                    data.AddRange(q.Data);

                return data.ToArray();
            }
        }
    }
}