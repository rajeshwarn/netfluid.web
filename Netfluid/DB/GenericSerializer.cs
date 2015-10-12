using System;
using System.Text;
using Netfluid;

namespace Netfluid.Db
{
    class GenericSerializer : ISerializer<object>
    {
        readonly Type Type;

        public GenericSerializer(Type type)
        {
            Type = type;
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public int Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Deserialize(byte[] buffer, int offset, int length)
        {
            if (Type == typeof(bool)) return BitConverter.ToBoolean(buffer, offset);
            if (Type == typeof(float)) return BitConverter.ToSingle(buffer, offset);
            if (Type == typeof(string)) return Encoding.UTF8.GetString(buffer, offset, length);
            if (Type == typeof(DateTime)) return new DateTime(BitConverter.ToInt64(buffer, offset));
            if (Type.IsEnum) return Enum.Parse(Type,Encoding.UTF8.GetString(buffer, offset, length));
            if (Type == typeof(double)) return BitConverter.ToDouble(buffer, offset);
            if (Type == typeof(int)) return BitConverter.ToInt32(buffer, offset);
            if (Type == typeof(char)) return BitConverter.ToChar(buffer, offset);
            if (Type == typeof(long)) return BitConverter.ToInt64(buffer, offset);
            if (Type == typeof(short)) return BitConverter.ToInt16(buffer, offset);
            if (Type == typeof(ushort)) return BitConverter.ToUInt16(buffer, offset);
            if (Type == typeof(uint)) return BitConverter.ToUInt32(buffer, offset);
            if (Type == typeof(ulong)) return BitConverter.ToUInt64(buffer, offset);
            if (Type == typeof(decimal)) return decimal.Parse(Encoding.UTF8.GetString(buffer, offset, length));

            var arr = new byte[length];
            Array.Copy(buffer, offset, arr, 0, length);
            return BSON.Deserialize(arr);
        }

        public byte[] Serialize(object value)
        {
            if (Type == typeof(bool)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(float)) return BitConverter.GetBytes((float)value);

            if (Type == typeof(string) || Type.IsEnum) return Encoding.UTF8.GetBytes(value.ToString());
            if (Type == typeof(DateTime)) return BitConverter.GetBytes(((DateTime)value).Ticks);

            if (Type == typeof(double)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(int)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(char)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(long)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(short)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(ushort)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(uint)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(ulong)) return BitConverter.GetBytes((bool)value);
            if (Type == typeof(decimal)) return Encoding.UTF8.GetBytes(value.ToString());

            return BSON.Serialize(value);
        }
    }
}
