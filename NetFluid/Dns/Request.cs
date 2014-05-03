using System;
using System.Collections.Generic;

namespace NetFluid.DNS.Records
{
    public class Request : List<Question>
    {
        public Header header;

        public Request()
        {
            header = new Header {OPCODE = OPCode.Query, QDCOUNT = 0, ID = (ushort) DateTime.Now.Millisecond};
        }

        public byte[] Write
        {
            get
            {
                var data = new List<byte>();
                header.QDCOUNT = (ushort) Count;
                data.AddRange(header.Data);

                foreach (Question q in this)
                    data.AddRange(q.Data);

                return data.ToArray();
            }
        }
    }
}