﻿// ********************************************************************************************************
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

using System.Xml.Schema;
using NetFluid.Serialization;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NetFluid
{
    public class JSON
    {

        public static void Serialize(object json, Stream stream, bool singlerow = false)
        {
            var writer=  new StreamWriter(stream);
            Serialize(json, writer, 0, singlerow);
            writer.Flush();
        }

        public static void Serialize(object json, TextWriter writer, bool singlerow = false, bool omitNull = false)
        {
            Serialize(json, writer, 0, singlerow,omitNull);
        }

        public static string Serialize(object json, bool singlerow = false)
        {
            var builder = new StringWriter();
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

        #region SERIALIZE

        private static void Serialize(object obj, TextWriter builder, int tab = 0, bool spaceless = false, bool omitNull=false)
        {
            string space = spaceless ? string.Empty : new string('\t', tab);

            if (obj == null)
            {
                builder.Write("null");
                return;
            }
            if (obj is string)
            {
                builder.Write(Escape(obj as string));
                return;
            }
            if (obj.GetType().IsArray || obj.GetType().Implements(typeof(IEnumerable)))
            {
                SerializeArray(obj as IEnumerable, builder, tab, spaceless,omitNull);
                return;
            }
            if (obj is TimeSpan)
            {
                builder.Write("\"" + ((TimeSpan)obj) + "\"");
                return;
            }
            if (obj is DateTimeOffset)
            {
                builder.Write("\"" + ((DateTimeOffset)obj).ToUniversalTime() + "\"");
                return;
            }
            if (obj is DateTime)
            {
                builder.Write("\"" + ((DateTime)obj).ToUniversalTime() + "\"");
                return;
            }
            if (obj is Enum)
            {
                builder.Write("\"" + obj + "\"");
                return;
            }
            if (obj is IConvertible)
            {
                builder.Write(Escape(obj.ToString().ToLower()));
                return;
            }
            if (obj is Guid)
            {
                builder.Write(obj);
                return;
            }


            if (spaceless)
                builder.Write("{");
            else
                builder.Write("\r\n" + space + "{\r\n");


            var type = obj.GetType();

            /*if (spaceless)
                builder.Write("\"$type\" :" + "\"" + type.FullName + "\",");
            else
                builder.Write(space + "\"$type\" :" + "\"" + type.FullName + "\", \r\n");*/

            var props = type.GetProperties();
            var fields = type.GetFields();

            #region PROPERITIES

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

                    if (!omitNull || value != null)
                    {
                        builder.Write(space);

                        builder.Write(Escape(key));

                        builder.Write(":");

                        if (value == null)
                        {
                            builder.Write("null");
                        }
                        else
                        {
                            Serialize(value, builder, tab + 1, spaceless,omitNull);
                        }

                        if (i != (props.Length + fields.Length - 1))
                            builder.Write(",");

                        if (!spaceless)
                            builder.Write("\r\n");
                    }
                }
            }

            #endregion


            for (int i = 0; i < fields.Length; i++)
            {
                string key = fields[i].Name;
                object value = fields[i].GetValue(obj);

                if (!omitNull || value != null)
                {
                    builder.Write(space);

                    builder.Write(Escape(key));
                    builder.Write(":");

                    if (value == null)
                    {
                        builder.Write("null");
                    }
                    else
                    {
                        Serialize(value, builder, tab + 1, spaceless,omitNull);
                    }

                    if (i != (props.Length + fields.Length - 1))
                        builder.Write(",");

                    if (!spaceless)
                        builder.Write("\r\n");
                }
            }


            if (!spaceless)
                builder.Write(space + "}");
            else
                builder.Write("}");
        }

        private static void SerializeArray(IEnumerable enumerable, TextWriter builder, int tab = 0, bool spaceless = false,bool omitNull=false)
        {
            var enumerator = enumerable.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                builder.Write("[]");
                return;
            }

            string space = spaceless ? string.Empty : new string('\t', tab);

            if (spaceless)
                builder.Write("[");
            else
                builder.Write(space + "[");

            Serialize(enumerator.Current, builder, tab + 1, spaceless, omitNull);

            while (enumerator.MoveNext())
            {
                builder.Write(",");
                Serialize(enumerator.Current, builder, tab + 1, spaceless, omitNull);
            }

            if (!spaceless)
                builder.Write(space + "]");
            else
                builder.Write("]");
        }

        public static string Escape(string aString)
        {
            var sb = new StringBuilder();
            sb.Append("\"");

            char[] charArray = aString.ToCharArray();
            foreach (char c in charArray)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        int codepoint = Convert.ToInt32(c);
                        if ((codepoint >= 32) && (codepoint <= 126))
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                        }
                        break;
                }
            }

            sb.Append("\"");

            return sb.ToString();
        }

        #endregion
    }
}