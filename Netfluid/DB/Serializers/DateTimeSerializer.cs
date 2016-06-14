using System;
namespace Netfluid.DB.Serializers
{
    class DateTimeSerializer : ISerializer<DateTime>
    {
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

        public DateTime Deserialize(byte[] buffer, int offset, int length)
        {
            return new DateTime(BufferHelper.ReadBufferInt64(buffer, offset));
        }

        public byte[] Serialize(DateTime value)
        {
            return BitConverter.GetBytes(value.Ticks);
        }
    }
}
