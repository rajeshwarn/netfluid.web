using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace NetFluid
{
    /// <summary>
    /// HTTP Header.It can be an array
    /// </summary>
    public sealed class WebHeader : IConvertible, IEnumerable<string>
    {
        private string[] values;

        #region CTOR

        public WebHeader()
        {
            Name = "";
            values = new string[0];
        }

        /// <summary>
        /// A new header with name and value
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="str">value</param>
        public WebHeader(string name, string str)
        {
            Name = name;
            values = new[] {str};
        }

        /// <summary>
        /// New header with multiple values (ex: cookies)
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="str">values</param>
        public WebHeader(string name, IEnumerable<string> str)
        {
            Name = name;
            values = str.ToArray();
        }

        #endregion

        /// <summary>
        /// Name of the header
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// True if is an array
        /// </summary>
        public bool IsMultiple
        {
            get { return values.Length >= 2; }
        }

        /// <summary>
        /// The value of the header.JSON serialized if is an array
        /// </summary>
        public string Value
        {
            get
            {
                switch (values.Length)
                {
                    case 0:
                        return "";
                    case 1:
                        return values[0];
                    default:
                        return "[" + string.Join(",", values.Select(JSON.Escape)) + "]";
                }
            }
        }

        /// <summary>
        /// Return array values
        /// </summary>
        /// <returns></returns>
        public string[] ToArray()
        {
            return values;
        }

        internal void Add(WebHeader q)
        {
            values = values.Concat(q.values).ToArray();
        }

        internal void Add(string s)
        {
            values = values.Concat(new[] {s}).ToArray();
        }

        /// <summary>
        /// The value of the header.JSON serialized if is an array
        /// </summary>
        public override string ToString()
        {
            switch (values.Length)
            {
                case 0:
                    return "null";
                case 1:
                    return values[0];
                default:
                    return ("[" + string.Join(",", values.Select(JSON.Escape)) + "]");
            }
        }

        public override int GetHashCode()
        {
            if (values.Length > 1)
                return string.Join(" ", values).GetHashCode();

            return values.Length == 0 ? string.Empty.GetHashCode() : values[0].GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj.ToString().Equals(ToString());
        }

        public object Parse(Type type)
        {
            var q = new QueryValue(Name,values);
            return q.Parse(type);
        }

        public bool StartsWith(string str)
        {
            return values.Any(x => x.StartsWith(str));
        }

        public bool Contains(string gzip)
        {
            return values.Any(x => x.Contains(gzip));
        }

        #region ICONVERTIBLE

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.String;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return values.Length != 0 && bool.Parse(values[0]);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return values.Length == 0 ? (byte) 0 : byte.Parse(values[0]);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return values.Length == 0 ? char.MinValue : char.Parse(values[0]);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return values.Length == 0 ? DateTime.MinValue : DateTime.Parse(values[0]);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : decimal.Parse(values[0]);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : double.Parse(values[0]);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return values.Length == 0 ? (short) 0 : short.Parse(values[0]);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : int.Parse(values[0]);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : long.Parse(values[0]);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return values.Length == 0 ? (sbyte) 0 : sbyte.Parse(values[0]);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : float.Parse(values[0]);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            if (values.Length > 1)
                return string.Join(" ", values);

            return values.Length == 0 ? string.Empty : values[0];
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return values.Length == 0 ? (ushort) 0 : ushort.Parse(values[0]);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : uint.Parse(values[0]);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return values.Length == 0 ? 0 : ulong.Parse(values[0]);
        }

        #endregion

        #region STRING CAST

        public static implicit operator string(WebHeader q)
        {
            return q.ToString();
        }

        public static implicit operator WebHeader(string q)
        {
            return new WebHeader("", q);
        }

        #endregion

        #region IEnumerable<string> Members

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return ((IEnumerable<string>) values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        #endregion
    }
}