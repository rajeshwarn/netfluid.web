using System;

namespace Netfluid.DB.Serializers
{
    class DoubleSerializer : ISerializer<double>
    {
        public byte[] Serialize(double value)
        {
            return BitConverter.GetBytes(value);
        }

        public double Deserialize(byte[] buffer, int offset, int length)
        {
            if (length != 8)
            {
                throw new ArgumentException("Invalid length: " + length);
            }

            return BitConverter.ToDouble(buffer, offset);
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
