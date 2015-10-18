using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.DB.Serializers
{
    class DoubleSerializer : ISerializer<double>
    {
        public byte[] Serialize(double value)
        {
            var bytes = BitConverter.GetBytes(value);

            if (false == BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public double Deserialize(byte[] buffer, int offset, int length)
        {
            if (length != 8)
            {
                throw new ArgumentException("Invalid length: " + length);
            }

            return BufferHelper.ReadBufferDouble(buffer, offset);
        }

        public bool IsFixedSize
        {
            get
            {
                return true;
            }
        }

        public int Length
        {
            get
            {
                return 8;
            }
        }
    }
}
