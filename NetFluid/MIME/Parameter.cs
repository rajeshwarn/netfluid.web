//
// Parameter.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013-2014 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Text;
using NetFluid.MIME.Encodings;
using NetFluid.MIME.Utils;

namespace NetFluid.MIME
{
    /// <summary>
    ///     A header parameter as found in the Content-Type and Content-Disposition headers.
    /// </summary>
    /// <remarks>
    ///     Content-Type and Content-Disposition headers often have parameters that specify
    ///     further information about how to interpret the content.
    /// </remarks>
    internal class Parameter
    {
        private string text;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Parameter" /> class.
        /// </summary>
        /// <remarks>
        ///     Creates a new parameter with the specified name and value.
        /// </remarks>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="name" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="value" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     The <paramref name="name" /> contains illegal characters.
        /// </exception>
        public Parameter(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException("Parameter names are not allowed to be empty.", "name");

            if (name.Any(t => t > 127 || !IsAttr((byte) t)))
            {
                throw new ArgumentException("Illegal characters in parameter name.", "name");
            }

            if (value == null)
                throw new ArgumentNullException("value");

            Name = name;
            Value = value;
        }

        /// <summary>
        ///     Gets the parameter name.
        /// </summary>
        /// <remarks>
        ///     Gets the parameter name.
        /// </remarks>
        /// <value>The parameter name.</value>
        public string Name { get; private set; }

        /// <summary>
        ///     Gets or sets the parameter value.
        /// </summary>
        /// <remarks>
        ///     Gets or sets the parameter value.
        /// </remarks>
        /// <value>The parameter value.</value>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value" /> is <c>null</c>.
        /// </exception>
        public string Value
        {
            get { return text; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (text == value)
                    return;

                text = value;
                OnChanged();
            }
        }

        private static bool IsAttr(byte c)
        {
            return c.IsAttr();
        }

        private static EncodeMethod GetEncodeMethod(FormatOptions options, string name, string value, out string quoted)
        {
            var method = EncodeMethod.None;

            quoted = null;

            if (name.Length + 1 + value.Length >= options.MaxLineLength)
                return EncodeMethod.Rfc2184;

            foreach (char t in value)
            {
                if (t >= 127 || ((byte) t).IsCtrl())
                    return EncodeMethod.Rfc2184;

                if (!((byte) t).IsAttr())
                    method = EncodeMethod.Quote;
            }

            if (method == EncodeMethod.Quote)
            {
                quoted = MimeUtils.Quote(value);

                if (name.Length + 1 + quoted.Length >= options.MaxLineLength)
                    return EncodeMethod.Rfc2184;
            }

            return method;
        }

        private static EncodeMethod GetEncodeMethod(char[] value, int startIndex, int length)
        {
            var method = EncodeMethod.None;

            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (value[i] >= 127 || ((byte) value[i]).IsCtrl())
                    return EncodeMethod.Rfc2184;

                if (!((byte) value[i]).IsAttr())
                    method = EncodeMethod.Quote;
            }

            return method;
        }

        private static EncodeMethod GetEncodeMethod(byte[] value, int length)
        {
            var method = EncodeMethod.None;

            for (int i = 0; i < length; i++)
            {
                if (value[i] >= 127 || value[i].IsCtrl())
                    return EncodeMethod.Rfc2184;

                if (!value[i].IsAttr())
                    method = EncodeMethod.Quote;
            }

            return method;
        }

        private static bool IsCtrl(char c)
        {
            return ((byte) c).IsCtrl();
        }

        private static Encoding GetBestEncoding(string value, Encoding defaultEncoding)
        {
            int encoding = 0; // us-ascii

            foreach (char t in value)
            {
                if (t < 127)
                {
                    if (IsCtrl(t))
                        encoding = Math.Max(encoding, 1);
                }
                else if (t < 256)
                {
                    encoding = Math.Max(encoding, 1);
                }
                else
                {
                    encoding = 2;
                }
            }

            switch (encoding)
            {
                case 0:
                    return Encoding.ASCII;
                case 1:
                    return Encoding.GetEncoding(28591); // iso-8859-1
                default:
                    return defaultEncoding;
            }
        }

        private static bool GetNextValue(string charset, Encoder encoder, HexEncoder hex, char[] chars, ref int index,
            ref byte[] bytes, ref byte[] encoded, int maxLength, out string value)
        {
            int length = chars.Length - index;

            if (length < maxLength)
            {
                switch (GetEncodeMethod(chars, index, length))
                {
                    case EncodeMethod.Quote:
                        value = MimeUtils.Quote(new string(chars, index, length));
                        index += length;
                        return false;
                    case EncodeMethod.None:
                        value = new string(chars, index, length);
                        index += length;
                        return false;
                }
            }

            length = Math.Min(maxLength, length);

            do
            {
                int count = encoder.GetByteCount(chars, index, length, true);
                int ratio;
                if (count > maxLength && length > 1)
                {
                    ratio = (int) Math.Round(count/(double) length);
                    length -= Math.Max((count - maxLength)/ratio, 1);
                    continue;
                }

                if (bytes.Length < count)
                    Array.Resize(ref bytes, count);

                count = encoder.GetBytes(chars, index, length, bytes, 0, true);

                // Note: the first chunk needs to be encoded in order to declare the charset
                if (index > 0 || charset == "us-ascii")
                {
                    EncodeMethod method = GetEncodeMethod(bytes, count);

                    if (method == EncodeMethod.Quote)
                    {
                        value = MimeUtils.Quote(Encoding.ASCII.GetString(bytes, 0, count));
                        index += length;
                        return false;
                    }

                    if (method == EncodeMethod.None)
                    {
                        value = Encoding.ASCII.GetString(bytes, 0, count);
                        index += length;
                        return false;
                    }
                }

                int n = hex.EstimateOutputLength(count);
                if (encoded.Length < n)
                    Array.Resize(ref encoded, n);

                // only the first value gets a charset declaration
                int charsetLength = index == 0 ? charset.Length + 2 : 0;

                n = hex.Encode(bytes, 0, count, encoded);
                if (n > 3 && (charsetLength + n) > maxLength)
                {
                    int x = 0;

                    for (int i = n - 1; i >= 0 && charsetLength + i >= maxLength; i--)
                    {
                        if (encoded[i] == (byte) '%')
                            x--;
                        else
                            x++;
                    }

                    ratio = (int) Math.Round(count/(double) length);
                    length -= Math.Max(x/ratio, 1);
                    continue;
                }

                if (index == 0)
                    value = charset + "''" + Encoding.ASCII.GetString(encoded, 0, n);
                else
                    value = Encoding.ASCII.GetString(encoded, 0, n);
                index += length;
                return true;
            } while (true);
        }

        internal void Encode(FormatOptions options, StringBuilder builder, ref int lineLength, Encoding encoding)
        {
            string quoted;

            EncodeMethod method = GetEncodeMethod(options, Name, Value, out quoted);
            if (method == EncodeMethod.None)
                quoted = Value;

            if (method != EncodeMethod.Rfc2184)
            {
                if (lineLength + 2 + Name.Length + 1 + quoted.Length >= options.MaxLineLength)
                {
                    builder.Append(";\n\t");
                    lineLength = 1;
                }
                else
                {
                    builder.Append("; ");
                    lineLength += 2;
                }

                lineLength += Name.Length + 1 + quoted.Length;
                builder.Append(Name);
                builder.Append('=');
                builder.Append(quoted);
                return;
            }

            int maxLength = options.MaxLineLength - (Name.Length + 6);
            Encoding bestEncoding = GetBestEncoding(Value, encoding);
            string charset = CharsetUtils.GetMimeCharset(bestEncoding);
            var bytes = new byte[Math.Max(maxLength, 6)];
            var hexbuf = new byte[bytes.Length*3 + 3];
            Encoder encoder = bestEncoding.GetEncoder();
            char[] chars = Value.ToCharArray();
            var hex = new HexEncoder();
            int index = 0, i = 0;

            do
            {
                string value;
                bool encoded = GetNextValue(charset, encoder, hex, chars, ref index, ref bytes, ref hexbuf, maxLength,
                    out value);
                int length = Name.Length + (encoded ? 1 : 0) + 1 + value.Length;

                if (i == 0 && index == chars.Length)
                {
                    if (lineLength + 2 + length >= options.MaxLineLength)
                    {
                        builder.Append(";\n\t");
                        lineLength = 1;
                    }
                    else
                    {
                        builder.Append("; ");
                        lineLength += 2;
                    }

                    builder.Append(Name);
                    if (encoded)
                        builder.Append('*');
                    builder.Append('=');
                    builder.Append(value);
                    lineLength += length;
                    return;
                }

                builder.Append(";\n\t");
                lineLength = 1;

                string id = i.ToString();
                length += id.Length + 1;

                builder.Append(Name);
                builder.Append('*');
                builder.Append(id);
                if (encoded)
                    builder.Append('*');
                builder.Append('=');
                builder.Append(value);
                lineLength += length;
                i++;
            } while (index < chars.Length);
        }

        /// <summary>
        ///     Returns a string representation of the <see cref="Parameter" />.
        /// </summary>
        /// <remarks>
        ///     Formats the parameter name and value in the form <c>name="value"</c>.
        /// </remarks>
        /// <returns>A string representation of the <see cref="Parameter" />.</returns>
        public override string ToString()
        {
            return Name + "=" + MimeUtils.Quote(Value);
        }

        internal event EventHandler Changed;

        private void OnChanged()
        {
            if (Changed != null)
                Changed(this, EventArgs.Empty);
        }

        private enum EncodeMethod
        {
            None,
            Quote,
            Rfc2184
        }
    }
}