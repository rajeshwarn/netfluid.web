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

using NetFluid.Serialization;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NetFluid
{
    public class JSON
    {
        public static string Serialize(object json, bool singlerow = false)
        {
            var builder = new StringBuilder();
            Serialize(json, builder, 0, singlerow);
            return builder.ToString();
        }

        public static object Deserialize(string json, Type type)
        {
            var parser = new JsonParser(json);
            return JsonParser.Translate(type, parser.ParseValue());
        }


        public static T Deserialize<T>(string json)
        {
            var parser = new JsonParser(json);
            return (T) JsonParser.Translate(typeof (T), parser.ParseValue());
        }

        private static object Deserialize(ref string json, Type type)
        {
            json = json.Trim();

            if (json[0] == ':' || json[0] == ',')
            {
                json = json.Substring(1);
                json = json.Trim();
            }

            if (type.IsEnum)
            {
                string[] names = type.GetEnumNames();
                Array values = type.GetEnumValues();

                foreach (string k in names.OrderByDescending(x => x.Length))
                {
                    if (json.StartsWith(k))
                    {
                        json = json.Substring(k.Length);

                        for (int i = 0; i < names.Length; i++)
                        {
                            if (names[i] == k)
                                return values.GetValue(i);
                        }
                    }
                }
            }

            switch (json[0])
            {
                case '"':
                    return ParseString(ref json);
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    json = json.Trim();
                    int lastIndex;
                    for (lastIndex = 0; lastIndex < json.Length; lastIndex++)
                    {
                        if (!"0123456789+-.: /eE".Contains("" + json[lastIndex]))
                        {
                            break;
                        }
                    }

                    string snum = json.Substring(0, lastIndex);
                    json = json.Substring(snum.Length);

                    if (type == typeof (byte))
                        return byte.Parse(snum);
                    if (type == typeof (sbyte))
                        return sbyte.Parse(snum);
                    if (type == typeof (UInt16))
                        return UInt16.Parse(snum);
                    if (type == typeof (UInt32))
                        return UInt32.Parse(snum);
                    if (type == typeof (UInt64))
                        return UInt64.Parse(snum);
                    if (type == typeof (Int16))
                        return Int16.Parse(snum);
                    if (type == typeof (Int32))
                        return Int32.Parse(snum);
                    if (type == typeof (Int64))
                        return Int64.Parse(snum);
                    if (type == typeof (float))
                        return float.Parse(snum);
                    if (type == typeof (double))
                        return double.Parse(snum);
                    if (type == typeof (decimal))
                        return decimal.Parse(snum);
                    if (type == typeof (DateTime))
                    {
                        long ival;

                        if (long.TryParse(snum, out ival))
                        {
                            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            return origin.AddSeconds(ival);
                        }

                        return DateTime.Parse(snum);
                    }
                    if (type == typeof (Guid))
                        return Guid.Parse(snum);

                    MethodInfo method = type.GetMethod("Parse", BindingFlags.Static);
                    if (method != null && method.GetParameters().Length == 1 &&
                        method.GetParameters().First().ParameterType == typeof (string))
                    {
                        return method.Invoke(null, new object[] {snum});
                    }

                    break;

                case '[':

                    var array = new ArrayList();

                    Type elemType = type.GetElementType();
                    if (json.StartsWith("[]"))
                    {
                        return Array.CreateInstance(elemType, 0);
                    }

                    json = json.Substring(1);

                    while (json[0] != ']')
                    {
                        json = json.Trim();
                        object value = Deserialize(ref json, elemType);
                        array.Add(value);
                        json = json.Trim();
                    }
                    json = json.Substring(1);

                    Array ret = Array.CreateInstance(elemType, array.Count);

                    for (int i = 0; i < array.Count; i++)
                    {
                        ret.SetValue(array[i], i);
                    }

                    return ret;

                case '{':

                    #region OBJECT

                    json = json.Substring(1);
                    object obj = type.CreateIstance();

                    while (json[0] != '}')
                    {
                        string name = ParseString(ref json);

                        if (name == null)
                            return obj;

                        #region $type

                        if (name.StartsWith("$"))
                        {
                            string typename = ParseString(ref json);

                            Type t = Type.GetType(typename);

                            if (t != null)
                            {
                                type = t;
                                obj = t.CreateIstance();
                            }
                        }

                        #endregion

                        FieldInfo fi = type.GetField(name);

                        if (fi != null)
                        {
                            fi.SetValue(obj, Deserialize(ref json, fi.FieldType));
                        }
                        else
                        {
                            #region PROP

                            PropertyInfo pi = type.GetProperty(name);

                            if (pi != null)
                            {
                                object value = Deserialize(ref json, pi.PropertyType);
                                MethodInfo pset = pi.GetSetMethod(true);

                                if (pset != null)
                                {
                                    pset.Invoke(obj, new[] {value});
                                }
                            }

                            #endregion
                        }
                        json = json.Trim();
                    }
                    json = json.Substring(1);
                    return obj;

                    #endregion

                case 'T':
                case 't':
                    if (json.ToLower().StartsWith("true"))
                    {
                        json = json.Substring("true".Length);
                        return true;
                    }
                    break;
                case 'F':
                case 'f':
                    if (json.ToLower().StartsWith("false"))
                    {
                        json = json.Substring("false".Length);
                        return false;
                    }
                    break;
                case 'n':
                    if (json.StartsWith("null"))
                    {
                        json = json.Substring("null".Length);
                        return null;
                    }
                    break;
            }
            return null;
        }

        private static string ParseString(ref string json)
        {
            const string regexQuote = "^@\"(?:[^\"]+|\"\")*\"|\"(?:[^\"\\\\]+|\\\\.)*\"";

            Match match = Regex.Match(json, regexQuote);

            if (!match.Success)
                return null;

            json = json.Substring(match.Index + match.Value.Length);

            string str =
                match.Value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\b", "\b").Replace("\\f", "\f").
                    Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");

            foreach (Match m in Regex.Matches(str, "\\\\u[0-9a-fA-F]+"))
            {
                int codePoint;
                if (Int32.TryParse(m.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint))
                {
                    str = str.Replace(m.Value, Char.ConvertFromUtf32(codePoint));
                }
            }

            str = str.Substring(1);
            str = str.Substring(0, str.Length - 1);

            return str;
        }

        #region SERIALIZE

        private static void Serialize(object obj, StringBuilder builder, int tab = 0, bool spaceless = false)
        {
            string space = spaceless ? string.Empty : new string('\t', tab);

            if (obj == null)
            {
                builder.Append("null");
                return;
            }
            if (obj is string)
            {
                SerializeString(obj as string, builder);
                return;
            }
            if (obj.GetType().IsArray)
            {
                SerializeArray(obj as Array, builder, tab, spaceless);
                return;
            }
            if (obj is DateTime)
            {
                builder.Append(((DateTime) obj).ToUniversalTime().ToString());
                return;
            }
            if (obj is Enum)
            {
                builder.Append("\"" + obj + "\"");
                return;
            }
            if (obj is IConvertible)
            {
                builder.Append(obj.ToString().ToLower());
                return;
            }
            if (obj is Guid)
            {
                builder.Append(obj);
                return;
            }


            if (spaceless)
                builder.Append("{");
            else
                builder.Append("\r\n" + space + "{\r\n");


            Type type = obj.GetType();

            if (spaceless)
                builder.Append("\"$type\" :" + "\"" + type.FullName + "\",");
            else
                builder.Append(space + "\"$type\" :" + "\"" + type.FullName + "\", \r\n");

            #region PROPERITIES

            PropertyInfo[] props = type.GetProperties();

            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].GetIndexParameters().Length == 0)
                {
                    string key = props[i].Name;

                    object value = null;

                    try
                    {
                        value = props[i].GetGetMethod().Invoke(obj, null);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    builder.Append(space);

                    SerializeString(key, builder);
                    builder.Append(":");

                    if (value == null)
                    {
                        builder.Append("null");
                    }
                    else
                    {
                        Serialize(value, builder, tab + 1, spaceless);
                    }

                    if (i != (props.Length - 1))
                        builder.Append(",");

                    if (!spaceless)
                        builder.Append("\r\n");
                }
            }

            #endregion

            FieldInfo[] fields = type.GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                string key = fields[i].Name;
                object value = fields[i].GetValue(obj);

                builder.Append(space);

                SerializeString(key, builder);
                builder.Append(":");

                if (value == null)
                {
                    builder.Append("null");
                }
                else
                {
                    Serialize(value, builder, tab + 1, spaceless);
                }

                if (i != (fields.Length - 1))
                    builder.Append(",");

                if (!spaceless)
                    builder.Append("\r\n");
            }


            if (!spaceless)
                builder.Append(space + "}");
            else
                builder.Append("}");
        }

        private static void SerializeArray(Array anArray, StringBuilder builder, int tab = 0, bool spaceless = false)
        {
            if (anArray.Length == 0)
            {
                builder.Append("[]");
                return;
            }

            string space = spaceless ? string.Empty : new string('\t', tab);

            if (spaceless)
                builder.Append("[");
            else
                builder.Append(space + "\r\n[\r\n");

            for (int i = 0; i < anArray.Length; i++)
            {
                object value = anArray.GetValue(i);

                Serialize(value, builder, tab + 1, spaceless);

                if (i < (anArray.Length - 1))
                    builder.Append(", ");

                if (!spaceless)
                {
                    builder.Append("\r\n");
                }
            }

            if (!spaceless)
                builder.Append(space + "]");
            else
                builder.Append("]");
        }

        private static void SerializeString(string aString, StringBuilder builder)
        {
            builder.Append("\"");

            char[] charArray = aString.ToCharArray();
            foreach (char c in charArray)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        int codepoint = Convert.ToInt32(c);
                        if ((codepoint >= 32) && (codepoint <= 126))
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                        }
                        break;
                }
            }

            builder.Append("\"");
        }

        #endregion
    }
}