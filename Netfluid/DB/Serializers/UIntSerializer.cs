using System;

namespace Netfluid.DB.Serializers
{
    internal class UIntSerializer : ISerializer<uint>
    {
        public byte[] Serialize(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        public uint Deserialize(byte[] buffer, int offset, int length)
        {
            if (length != 4)
            {
                throw new ArgumentException("Invalid length: " + length);
            }

            return BufferHelper.ReadBufferUInt32(buffer, offset);
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
                return 4;
            }
        }
    }
}
