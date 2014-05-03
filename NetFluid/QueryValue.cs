// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetFluid
{
    public sealed class QueryValue : IConvertible, IEnumerable<string>
    {
        private string[] values;

        #region CTOR

        public QueryValue()
        {
            Name = "";
            values = new string[0];
        }

        public QueryValue(string name, string str)
        {
            Name = name;
            values = new[] {str};
        }

        #endregion

        public string Name { get; private set; }

        public bool IsMultiple
        {
            get { return values.Length >= 2; }
        }

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
                        return "[" + string.Join(",", values) + "]";
                }
            }
        }

        public string[] ToArray()
        {
            return values;
        }

        internal void Add(QueryValue q)
        {
            values = values.Concat(q.values).ToArray();
        }

        internal void Add(string s)
        {
            values = values.Concat(new[] {s}).ToArray();
        }

        public override string ToString()
        {
            var b = new StringBuilder(JSON.Escape(Name) + ":");

            switch (values.Length)
            {
                case 0:
                    b.Append("null");
                    break;
                case 1:
                    b.Append(JSON.Escape(values[0]));
                    break;
                default:
                    b.Append("[" + string.Join(",", values.Select(JSON.Escape)) + "]");
                    break;
            }

            return b.ToString();
            /*if (values.Length > 1)
                return string.Join(" ", values);

            return values.Length == 0 ? string.Empty : values[0];*/
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

        internal object Parse(ParameterInfo x)
        {
            if (x.ParameterType.IsArray || x.GetCustomAttributes(typeof (ParamArrayAttribute), false).Length > 0)
            {
                #region ARRAY

                Type elemType = x.ParameterType.GetElementType();
                if (elemType == typeof (string))
                    return values;
                if (elemType == typeof (byte))
                    return (values.Select(byte.Parse)).ToArray();
                if (elemType == typeof (char))
                    return (values.Select(char.Parse)).ToArray();
                if (elemType == typeof (decimal))
                    return (values.Select(decimal.Parse)).ToArray();
                if (elemType == typeof (Int16))
                    return (values.Select(Int16.Parse)).ToArray();
                if (elemType == typeof (UInt16))
                    return (values.Select(UInt16.Parse)).ToArray();
                if (elemType == typeof (Int32))
                    return (values.Select(Int32.Parse)).ToArray();
                if (elemType == typeof (UInt32))
                    return (values.Select(UInt32.Parse)).ToArray();
                if (elemType == typeof (Int64))
                    return (values.Select(Int64.Parse)).ToArray();
                if (elemType == typeof (UInt64))
                    return (values.Select(UInt64.Parse)).ToArray();
                if (elemType == typeof (float))
                    return (values.Select(float.Parse)).ToArray();
                if (elemType == typeof (double))
                    return (values.Select(double.Parse)).ToArray();
                if (elemType == typeof (DateTime))
                    return (values.Select(DateTime.Parse)).ToArray();

                if (elemType == typeof (bool))
                {
                    return values.Select(y =>
                    {
                        string t = y.ToLower(CultureInfo.InvariantCulture);
                        return (t == "true" || t == "on" || t == "yes");
                    }).ToArray();
                }

                if (elemType.IsEnum)
                    return values.Select(y => Enum.Parse(elemType, y)).ToArray();

                MethodInfo parsemethod = elemType.GetMethod("Parse",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (parsemethod != null)
                {
                    Array r = Array.CreateInstance(elemType, values.Length);

                    for (int i = 0; i < r.Length; i++)
                    {
                        object o = parsemethod.Invoke(null, new[] {values[i]});
                        r.SetValue(o, i);
                    }

                    return r;
                }

                #endregion
            }

            #region VALORI

            Type myType = x.ParameterType;

            if (values.Length == 0 || (values.Length == 1 && string.IsNullOrEmpty(values[0])))
                return myType.IsValueType ? Activator.CreateInstance(myType) : null;

            if (myType == typeof (string))
                return values[0];
            if (myType == typeof (byte))
                return byte.Parse(values[0]);
            if (myType == typeof (char))
                return char.Parse(values[0]);
            if (myType == typeof (decimal))
                return decimal.Parse(values[0]);
            if (myType == typeof (Int16))
                return Int16.Parse(values[0]);
            if (myType == typeof (UInt16))
                return UInt16.Parse(values[0]);
            if (myType == typeof (Int32))
                return Int32.Parse(values[0]);
            if (myType == typeof (UInt32))
                return UInt32.Parse(values[0]);
            if (myType == typeof (Int64))
                return Int64.Parse(values[0]);
            if (myType == typeof (UInt64))
                return UInt64.Parse(values[0]);
            if (myType == typeof (float))
                return float.Parse(values[0]);
            if (myType == typeof (double))
                return double.Parse(values[0]);
            if (myType == typeof (DateTime))
                return DateTime.Parse(values[0]);

            if (myType == typeof (bool))
            {
                string t = values[0].ToLower(CultureInfo.InvariantCulture);
                return t == "true" || t == "on" || t == "yes";
            }

            if (myType.IsEnum)
                return Enum.Parse(myType, values[0]);

            MethodInfo method = myType.GetMethod("Parse",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
                return method.Invoke(null, new[] {values[0]});

            #endregion

            return myType.IsValueType ? Activator.CreateInstance(myType) : null;
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

        public static implicit operator string(QueryValue q)
        {
            return q.ToString();
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