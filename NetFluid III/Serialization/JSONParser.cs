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

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

#endregion

namespace NetFluid.Serialization
{
    internal class JsonParser
    {
        private readonly char[] json;
        private readonly StringBuilder s = new StringBuilder();
        private int index;
        private Token lookAheadToken = Token.None;


        internal JsonParser(string json)
        {
            this.json = json.ToCharArray();
        }

        private Dictionary<string, object> ParseObject()
        {
            var table = new Dictionary<string, object>();

            lookAheadToken = Token.None; // {

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comma:
                        lookAheadToken = Token.None;
                        break;

                    case Token.Curly_Close:
                        lookAheadToken = Token.None;
                        return table;

                    default:
                        {
                            // name
                            string name = ParseString();

                            // :
                            if (NextToken() != Token.Colon)
                            {
                                throw new Exception("Expected colon at index " + index);
                            }

                            // value
                            object value = ParseValue();

                            table[name] = value;
                        }
                        break;
                }
            }
        }

        private ArrayList ParseArray()
        {
            var array = new ArrayList();
            lookAheadToken = Token.None; // [

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comma:
                        lookAheadToken = Token.None;
                        break;

                    case Token.Squared_Close:
                        lookAheadToken = Token.None;
                        return array;

                    default:
                        {
                            array.Add(ParseValue());
                        }
                        break;
                }
            }
        }

        public static object ParseType(Type type, string json)
        {
            var parser = new JsonParser(json);

            return Translate(type, parser.ParseValue());
        }

        public static object Translate(Type type, object obj)
        {
            if (type.IsArray && obj is ArrayList)
            {
                var list = obj as ArrayList;
                Type subtype = type.GetElementType();
                Array returned = Array.CreateInstance(subtype, list.Count);

                for (int i = 0; i < returned.Length; i++)
                {
                    returned.SetValue(Translate(subtype, list[i]), i);
                }

                return returned;
            }

            if (type.IsArray && obj is Dictionary<string, object>)
            {
                Type subtype = type.GetElementType();
                object[] returned =
                    (obj as Dictionary<string, object>).Values.Select(x => Translate(subtype, x)).ToArray();
                return returned;
            }

            if (type.Implements(typeof (IDictionary<,>)) && obj.GetType().Implements(typeof (IDictionary<,>)))
            {
                Type[] interfaces = type.GetInterfaces();
                foreach (Type i in interfaces)
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IDictionary<,>))
                    {
                        Type tkey = i.GetGenericArguments()[0];
                        Type tvalue = i.GetGenericArguments()[1];

                        var fromDic = obj as IDictionary;
                        var toDic = type.CreateIstance() as IDictionary;

                        if (fromDic != null)
                            foreach (object key in fromDic.Keys)
                            {
                                if (toDic != null) toDic.Add(Translate(tkey, key), Translate(tvalue, fromDic[key]));
                            }

                        return toDic;
                    }
                return null;
            }

            if (type.IsClass && obj is Dictionary<string, object>)
            {
                var dic = obj as Dictionary<string, object>;

                object returned;

                if (dic.ContainsKey("$type"))
                {
                    Type t = Type.GetType(dic["$type"] as string);

                    if (t != null)
                    {
                        type = t;
                        returned = t.CreateIstance();
                    }
                    else
                    {
                        returned = type.CreateIstance();
                    }
                }
                else
                {
                    returned = type.CreateIstance();
                }

                foreach (FieldInfo item in type.GetFields())
                {
                    if (dic.ContainsKey(item.Name))
                    {
                        item.SetValue(returned, Translate(item.FieldType, dic[item.Name]));
                    }
                }
                return returned;
            }

            if (obj is string)
            {
                var snum = obj as string;

                if (type == typeof (String))
                    return snum;
                if (type.IsEnum)
                    return Enum.Parse(type, snum);
                if (type == typeof (char))
                    return char.Parse(snum);
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
                    return float.Parse(snum, CultureInfo.InvariantCulture);
                if (type == typeof (double))
                    return double.Parse(snum, CultureInfo.InvariantCulture);
                if (type == typeof (decimal))
                    return decimal.Parse(snum, CultureInfo.InvariantCulture);
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
            }

            return null;
        }

        public object ParseValue()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    return ParseNumber();

                case Token.String:
                    return ParseString();

                case Token.Curly_Open:
                    return ParseObject();

                case Token.Squared_Open:
                    return ParseArray();

                case Token.True:
                    lookAheadToken = Token.None;
                    return true;

                case Token.False:
                    lookAheadToken = Token.None;
                    return false;

                case Token.Null:
                    lookAheadToken = Token.None;
                    return null;
            }
            //Console.WriteLine(new string(json,index,(json.Length-index)));
            throw new Exception("Unrecognized token at index" + index);
        }

        private string ParseString()
        {
            lookAheadToken = Token.None; // "

            s.Length = 0;

            int runIndex = -1;

            while (index < json.Length)
            {
                char c = json[index++];

                if (c == '"')
                {
                    if (runIndex != -1)
                    {
                        if (s.Length == 0)
                            return new string(json, runIndex, index - runIndex - 1);

                        s.Append(json, runIndex, index - runIndex - 1);
                    }
                    return s.ToString();
                }

                if (c != '\\')
                {
                    if (runIndex == -1)
                        runIndex = index - 1;

                    continue;
                }

                if (index == json.Length) break;

                if (runIndex != -1)
                {
                    s.Append(json, runIndex, index - runIndex - 1);
                    runIndex = -1;
                }

                switch (json[index++])
                {
                    case '"':
                        s.Append('"');
                        break;

                    case '\\':
                        s.Append('\\');
                        break;

                    case '/':
                        s.Append('/');
                        break;

                    case 'b':
                        s.Append('\b');
                        break;

                    case 'f':
                        s.Append('\f');
                        break;

                    case 'n':
                        s.Append('\n');
                        break;

                    case 'r':
                        s.Append('\r');
                        break;

                    case 't':
                        s.Append('\t');
                        break;

                    case 'u':
                        {
                            int remainingLength = json.Length - index;
                            if (remainingLength < 4) break;

                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint = ParseUnicode(json[index], json[index + 1], json[index + 2], json[index + 3]);
                            s.Append((char) codePoint);

                            // skip 4 chars
                            index += 4;
                        }
                        break;
                }
            }

            throw new Exception("Unexpectedly reached end of string");
        }

        private static uint ParseSingleChar(char c1, uint multipliyer)
        {
            uint p1 = 0;
            if (c1 >= '0' && c1 <= '9')
                p1 = (uint) (c1 - '0')*multipliyer;
            else if (c1 >= 'A' && c1 <= 'F')
                p1 = (uint) ((c1 - 'A') + 10)*multipliyer;
            else if (c1 >= 'a' && c1 <= 'f')
                p1 = (uint) ((c1 - 'a') + 10)*multipliyer;
            return p1;
        }

        private static uint ParseUnicode(char c1, char c2, char c3, char c4)
        {
            uint p1 = ParseSingleChar(c1, 0x1000);
            uint p2 = ParseSingleChar(c2, 0x100);
            uint p3 = ParseSingleChar(c3, 0x10);
            uint p4 = ParseSingleChar(c4, 1);

            return p1 + p2 + p3 + p4;
        }

        private string ParseNumber()
        {
            lookAheadToken = Token.None;

            // Need to start back one place because the first digit is also a token and would have been consumed
            int startIndex = index - 1;

            do
            {
                char c = json[index];

                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                {
                    if (++index == json.Length) throw new Exception("Unexpected end of string whilst parsing number");
                    continue;
                }

                break;
            } while (true);

            return new string(json, startIndex, index - startIndex);
        }

        private Token LookAhead()
        {
            if (lookAheadToken != Token.None) return lookAheadToken;
            return lookAheadToken = NextTokenCore();
        }

        private Token NextToken()
        {
            Token result = lookAheadToken != Token.None ? lookAheadToken : NextTokenCore();

            lookAheadToken = Token.None;

            return result;
        }

        private Token NextTokenCore()
        {
            char c;

            // Skip past whitespace
            do
            {
                c = json[index];

                if (c > ' ') break;
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;
            } while (++index < json.Length);

            if (index == json.Length)
            {
                throw new Exception("Reached end of string unexpectedly");
            }

            c = json[index];

            index++;

            //if (c >= '0' && c <= '9')
            //    return Token.Number;

            switch (c)
            {
                case '{':
                    return Token.Curly_Open;

                case '}':
                    return Token.Curly_Close;

                case '[':
                    return Token.Squared_Open;

                case ']':
                    return Token.Squared_Close;

                case ',':
                    return Token.Comma;

                case '"':
                    return Token.String;

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
                case '+':
                case '.':
                    return Token.Number;

                case ':':
                    return Token.Colon;

                case 'f':
                    if (json.Length - index >= 4 &&
                        json[index + 0] == 'a' &&
                        json[index + 1] == 'l' &&
                        json[index + 2] == 's' &&
                        json[index + 3] == 'e')
                    {
                        index += 4;
                        return Token.False;
                    }
                    break;

                case 't':
                    if (json.Length - index >= 3 &&
                        json[index + 0] == 'r' &&
                        json[index + 1] == 'u' &&
                        json[index + 2] == 'e')
                    {
                        index += 3;
                        return Token.True;
                    }
                    break;

                case 'n':
                    if (json.Length - index >= 3 &&
                        json[index + 0] == 'u' &&
                        json[index + 1] == 'l' &&
                        json[index + 2] == 'l')
                    {
                        index += 3;
                        return Token.Null;
                    }
                    break;
            }

            throw new Exception("Could not find token at index " + --index);
        }

        #region Nested type: Token

        private enum Token
        {
            None = -1, // Used to denote no Lookahead available
            Curly_Open,
            Curly_Close,
            Squared_Open,
            Squared_Close,
            Colon,
            Comma,
            String,
            Number,
            True,
            False,
            Null
        }

        #endregion
    }
}