using System;
using System.Collections.Generic;

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class Request : List<Question>
    {
        public Header Header;

        public Request()
        {
            Header = new Header {OPCODE = OPCode.Query, QDCOUNT = 0, ID = (ushort) DateTime.Now.Millisecond};
        }

        public byte[] Write
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