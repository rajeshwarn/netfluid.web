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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetFluid.HTTP
{
    internal static class HttpUtility
    {
        #region Methods

        private static readonly char[] HexChars = "0123456789abcdef".ToCharArray();

        private static void WriteCharBytes(IList buf, char ch)
        {
            if (ch > 255)
            {
                foreach (byte b in Encoding.UTF8.GetBytes(new[] {ch}))
                    buf.Add(b);
            }
            else
                buf.Add((byte) ch);
        }

        public static string UrlDecode(string s)
        {
            if (null == s)
                return null;

            if (s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
                return s;

            long len = s.Length;
            var bytes = new List<byte>();

            for (int i = 0; i < len; i++)
            {
                char ch = s[i];
                if (ch == '%' && i + 2 < len && s[i + 1] != '%')
                {
                    int xchar;
                    if (s[i + 1] == 'u' && i + 5 < len)
                    {
                        // unicode hex sequence
                        xchar = GetChar(s, i + 2, 4);
                        if (xchar != -1)
                        {
                            WriteCharBytes(bytes, (char) xchar);
                            i += 5;
                        }
                        else
                            WriteCharBytes(bytes, '%');
                    }
                    else if ((xchar = GetChar(s, i + 1, 2)) != -1)
                    {
                        WriteCharBytes(bytes, (char) xchar);
                        i += 2;
                    }
                    else
                    {
                        WriteCharBytes(bytes, '%');
                    }
                    continue;
                }

                WriteCharBytes(bytes, ch == '+' ? ' ' : ch);
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        private static int GetInt(byte b)
        {
            var c = (char) b;
            if (c >= '0' && c <= '9')
                return c - '0';

            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;

            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            return -1;
        }

        private static int GetChar(string str, int offset, int length)
        {
            int val = 0;
            int end = length + offset;
            for (int i = offset; i < end; i++)
            {
                char c = str[i];
                if (c > 127)
                    return -1;

                int current = GetInt((byte) c);
                if (current == -1)
                    return -1;
                val = (val << 4) + current;
            }

            return val;
        }

        public static string UrlEncode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            if (
                s.Where(c => (c < '0') || (c < 'A' && c > '9') || (c > 'Z' && c < 'a') || (c > 'z')).All(
                    c => NotEncoded(c)))
                return s;

            var enc = Encoding.UTF8;
            var bytes = new byte[enc.GetMaxByteCount(s.Length)];
            int realLen = enc.GetBytes(s, 0, s.Length, bytes, 0);
            return enc.GetString(UrlEncodeToBytes(bytes, 0, realLen));
        }

        private static bool NotEncoded(char c)
        {
            return (c == '!' || c == '\'' || c == '(' || c == ')' || c == '*' || c == '-' || c == '.' || c == '_');
        }

        private static void UrlEncodeChar(char c, Stream result, bool isUnicode)
        {
            if (c > 255)
            {
                int i = c;

                result.WriteByte((byte) '%');
                result.WriteByte((byte) 'u');
                int idx = i >> 12;
                result.WriteByte((byte) HexChars[idx]);
                idx = (i >> 8) & 0x0F;
                result.WriteByte((byte) HexChars[idx]);
                idx = (i >> 4) & 0x0F;
                result.WriteByte((byte) HexChars[idx]);
                idx = i & 0x0F;
                result.WriteByte((byte) HexChars[idx]);
                return;
            }

            if (c > ' ' && NotEncoded(c))
            {
                result.WriteByte((byte) c);
                return;
            }
            if (c == ' ')
            {
                result.WriteByte((byte) '+');
                return;
            }
            if ((c < '0') ||
                (c < 'A' && c > '9') ||
                (c > 'Z' && c < 'a') ||
                (c > 'z'))
            {
                if (isUnicode && c > 127)
                {
                    result.WriteByte((byte) '%');
                    result.WriteByte((byte) 'u');
                    result.WriteByte((byte) '0');
                    result.WriteByte((byte) '0');
                }
                else
                    result.WriteByte((byte) '%');

                int idx = c >> 4;
                result.WriteByte((byte) HexChars[idx]);
                idx = c & 0x0F;
                result.WriteByte((byte) HexChars[idx]);
            }
            else
                result.WriteByte((byte) c);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                return null;

            int len = bytes.Length;
            if (len == 0)
                return new byte[0];

            if (offset < 0 || offset >= len)
                throw new ArgumentOutOfRangeException("offset");

            if (count < 0 || count > len - offset)
                throw new ArgumentOutOfRangeException("count");

            var result = new MemoryStream(count);
            int end = offset + count;
            for (int i = offset; i < end; i++)
                UrlEncodeChar((char) bytes[i], result, false);

            return result.ToArray();
        }

        public static string HtmlDecode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            var res = s.Replace("&amp;", "&")
                       .Replace("&lt;", "<")
                       .Replace("&gt;", ">")
                       .Replace("&cent;", "¢")
                       .Replace("&pound;", "£")
                       .Replace("&yen;", "¥")
                       .Replace("&euro;", "€")

                       .Replace("&sect;", "§")
                       .Replace("&copy;", "©")
                       .Replace("&reg;", "®")
                       .Replace("&trade;", "™")
                       .Replace("&quot;", "\"");

            foreach (Match m in Regex.Matches(res, "&#(?<n>[0-9]+);"))
            {
                res = res.Replace(m.Value, "" + char.ConvertFromUtf32(int.Parse(m.Groups["n"].Value)));
            }

            return res;
        }

        public static string HtmlEncode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            var output = new StringBuilder();

            int len = s.Length;
            for (int i = 0; i < len; i++)
                switch (s[i])
                {
                    case '<':
                        output.Append("&lt;");
                    break;
                    case '>':
                        output.Append("&gt;");
                    break;
                    case '&':
                        output.Append("&amp;");
                    break;
                    case '¢':
                        output.Append("&cent;");
                    break;
                    case '£':
                        output.Append("&pound;");
                    break;
                    case '¥':
                        output.Append("&yen;");
                    break;
                    case '€':
                        output.Append("&euro;");
                    break;
                    case '§':
                        output.Append("&sect;");
                    break;
                    case '©':
                        output.Append("&copy;");
                    break;
                    case '®':
                        output.Append("&reg;");
                    break;
                    case '™':
                        output.Append("&trade;");
                    break;
                    case '"':
                        output.Append("&quot;");
                        break;
                    default:
                        if (s[i] > 159)
                        {
                            output.Append("&#");
                            output.Append(((int) s[i]).ToString(CultureInfo.InvariantCulture));
                            output.Append(";");
                        }
                        else
                        {
                            output.Append(s[i]);
                        }
                    break;
                }
            return output.ToString();
        }

        internal static string GetStatusDescription(StatusCode code)
        {
            switch ((int) code)
            {
                case 100:
                    return "Continue";
                case 101:
                    return "Switching Protocols";
                case 102:
                    return "Processing";
                case 200:
                    return "OK";
                case 201:
                    return "Created";
                case 202:
                    return "Accepted";
                case 203:
                    return "Non-Authoritative Information";
                case 204:
                    return "No Content";
                case 205:
                    return "Reset Content";
                case 206:
                    return "Partial Content";
                case 207:
                    return "Multi-Status";
                case 300:
                    return "Multiple Choices";
                case 301:
                    return "Moved Permanently";
                case 302:
                    return "Found";
                case 303:
                    return "See Other";
                case 304:
                    return "Not Modified";
                case 305:
                    return "Use Proxy";
                case 306:
                    return "Switch Proxy";
                case 307:
                    return "Temporary Redirect ";
                case 400:
                    return "Bad Request";
                case 401:
                    return "Unauthorized";
                case 402:
                    return "Payment Required";
                case 403:
                    return "Forbidden";
                case 404:
                    return "Not Found";
                case 405:
                    return "Method Not Allowed";
                case 406:
                    return "Not Acceptable";
                case 407:
                    return "Proxy Authentication Required";
                case 408:
                    return "Request Timeout";
                case 409:
                    return "Conflict";
                case 410:
                    return "Gone";
                case 411:
                    return "Length Required";
                case 412:
                    return "Precondition Failed";
                case 413:
                    return "Request Entity Too Large";
                case 414:
                    return "Request-URI Too Long";
                case 415:
                    return "Unsupported Media Type";
                case 416:
                    return "Requested Range Not Satisfiable";
                case 417:
                    return "Expectation Failed";
                case 418:
                    return "I'm a teapot";
                case 421:
                    return "There are too many connections from your internet address";
                case 422:
                    return "Unprocessable Entity";
                case 423:
                    return "Locked";
                case 424:
                    return "Failed Dependency";
                case 425:
                    return "Unordered Collection";
                case 426:
                    return "Upgrade Required";
                case 449:
                    return "Retry With";
                case 450:
                    return "Blocked by Windows Parental Controls";
                case 500:
                    return "Internal Server Error";
                case 501:
                    return "Not Implemented";
                case 502:
                    return "Bad Gateway";
                case 503:
                    return "Service Unavailable";
                case 504:
                    return "Gateway Timeout";
                case 505:
                    return "HTTP Version Not Supported";
                case 506:
                    return "Variant Also Negotiates";
                case 507:
                    return "Insufficient Storage";
                case 509:
                    return "Bandwidth Limit Exceeded";
                case 510:
                    return "Not Extended";
                case 530:
                    return "UserName access denied";
                case 666:
                    return "Lux Feror";
                case 999:
                    return "Any";
            }
            return "";
        }

        #endregion // Methods
    }
}