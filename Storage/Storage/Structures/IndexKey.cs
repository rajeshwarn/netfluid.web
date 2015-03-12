using System;
using System.Text;

namespace FluidDB
{
    internal enum IndexDataType
    { 
        Null,
        // Int
        Boolean,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        // Decimal
        Single,
        Double,
        Decimal,
        String,
        // Others
        DateTime,
        Guid
    };

    /// <summary>
    /// Represent a index key value - can be a string, int, decimal, guid, ... It's persistable
    /// </summary>
    internal struct IndexKey : IComparable<IndexKey>
    {
        public const int MAX_LENGTH_SIZE = 255;

        public readonly object Value;

        public readonly IndexDataType Type;

        public readonly bool IsNumber;

        public readonly int Length;

        public IndexKey(object value)
        {
            Value = value;
            IsNumber = false;

            // null
            if (value == null) { Type = IndexDataType.Null; Length = 0; }

            // int
            else if (value is Byte) { Type = IndexDataType.Byte; Length = 1; IsNumber = true; }
            else if (value is Int16) { Type = IndexDataType.Int16; Length = 2; IsNumber = true; }
            else if (value is UInt16) { Type = IndexDataType.UInt16; Length = 2; IsNumber = true; }
            else if (value is Int32) { Type = IndexDataType.Int32; Length = 4; IsNumber = true; }
            else if (value is UInt32) { Type = IndexDataType.UInt32; Length = 4; IsNumber = true; }
            else if (value is Int64) { Type = IndexDataType.Int64; Length = 8; IsNumber = true; }
            else if (value is UInt64) { Type = IndexDataType.UInt64; Length = 8; IsNumber = true; }

            // decimal
            else if (value is Single) { Type = IndexDataType.Single; Length = 4; IsNumber = true; }
            else if (value is Double) { Type = IndexDataType.Double; Length = 8; IsNumber = true; }
            else if (value is Decimal) { Type = IndexDataType.Decimal; Length = 16; IsNumber = true; }

            // string
            else if (value is String) { Type = IndexDataType.String; Length = Encoding.UTF8.GetByteCount((string)Value) + 1 /* +1 = For String Length on store */; }

            // Others
            else if (value is Boolean) { Type = IndexDataType.Boolean; Length = 1; }
            else if (value is DateTime) { Type = IndexDataType.DateTime; Length = 8; }
            else if (value is Guid) { Type = IndexDataType.Guid; Length = 16; }

            // if not found, exception
            else throw new LiteException(202, "The '{0}' datatype is not valid for index", value.GetType());

            // increase "Type" byte in length
            Length++;

            // withespace empty string == null
            if (Type == IndexDataType.String && ((string)value).Trim().Length == 0)
            {
                Value = null;
                Type = IndexDataType.Null;
                Length = 1;
            }

            // limit in 255 string bytes
            if (Type == IndexDataType.String && Length > MAX_LENGTH_SIZE)
            {   
                throw new LiteException(202, "Index key must be less than {0} bytes", IndexKey.MAX_LENGTH_SIZE);
            }
        }

        public int CompareTo(IndexKey other)
        {
            // first, compare Null values (null is always less than other type
            if (Type == IndexDataType.Null && other.Type == IndexDataType.Null) return 0;
            if (Type == IndexDataType.Null) return -1;
            if (other.Type == IndexDataType.Null) return 1;

            // if types are diferentes, convert
            if (Type != other.Type)
            {
                // if both values are number, convert them to Double to compare
                // using Double because it's faster then Decimal and bigger range
                if (IsNumber && other.IsNumber)
                {
                    return Convert.ToDouble(Value).CompareTo(Convert.ToDouble(other.Value));
                }

                // if not, convert both to string
                return string.Compare(Value.ToString(), other.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            // for both values with same datatype just compare

            // int
            if (Type == IndexDataType.Byte) return ((Byte)Value).CompareTo(other.Value);
            if (Type == IndexDataType.Int16) return ((Int16)Value).CompareTo(other.Value);
            if (Type == IndexDataType.UInt16) return ((UInt16)Value).CompareTo(other.Value);
            if (Type == IndexDataType.Int32) return ((Int32)Value).CompareTo(other.Value);
            if (Type == IndexDataType.UInt32) return ((UInt32)Value).CompareTo(other.Value);
            if (Type == IndexDataType.Int64) return ((Int64)Value).CompareTo(other.Value);
            if (Type == IndexDataType.UInt64) return ((UInt64)Value).CompareTo(other.Value);

            // decimal
            if (Type == IndexDataType.Single) return ((Single)Value).CompareTo(other.Value);
            if (Type == IndexDataType.Double) return ((Double)Value).CompareTo(other.Value);
            if (Type == IndexDataType.Decimal) return ((Decimal)Value).CompareTo(other.Value);

            // string
            if (Type == IndexDataType.String) return string.Compare((String)Value, other.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);

            // other
            if (Type == IndexDataType.Boolean) return ((Boolean)Value).CompareTo(other.Value);
            if (Type == IndexDataType.DateTime) return ((DateTime)Value).CompareTo(other.Value);
            if (Type == IndexDataType.Guid) return ((Guid)Value).CompareTo(other.Value);

 	        throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Value == null ? "(null)" : Value.ToString();
        }
    }
}
