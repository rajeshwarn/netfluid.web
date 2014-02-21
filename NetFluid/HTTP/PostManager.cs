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
using System.Globalization;
using System.IO;
using System.Text;

namespace NetFluid.HTTP
{
    internal static class PostManager
    {
        internal static void DecodeMultiPart(Context cnt, Stream stream)
        {
            string boundary = cnt.Request.ContentType.Substring(cnt.Request.ContentType.IndexOf('=') + 1);
            var multipart = new HttpMultipart(stream, boundary, cnt.Request.ContentEncoding);

            HttpMultipart.Element element;
            while ((element = multipart.ReadNextElement()) != null)
            {
                if (string.IsNullOrEmpty(element.Name))
                    throw new Exception("Bad request");

                if (!string.IsNullOrEmpty(element.Filename))
                {
                    if (string.IsNullOrEmpty(element.ContentType))
                        throw new Exception("Bad request");

                    // Read the file data
                    string path = Path.GetTempFileName();

                    var buffer = new byte[element.Length];
                    stream.Seek(element.Start, SeekOrigin.Begin);
                    stream.Read(buffer, 0, (int) element.Length);
                    File.WriteAllBytes(path, buffer);

                    var file = new HttpFile
                    {
                        TempFile = path,
                        Name = element.Name,
                        FileName = element.Filename,
                        ContentType = element.ContentType,
                        Extension =
                            element.Filename.Contains(".")
                                ? element.Filename.Substring(element.Filename.LastIndexOf('.'))
                                : "",
                        FileNameWithoutExtesion =
                            element.Filename.Contains(".")
                                ? element.Filename.Substring(0, element.Filename.LastIndexOf('.'))
                                : element.Filename,
                    };

                    if (cnt.Request.Files == null)
                        cnt.Request.Files = new HttpFileCollection();

                    cnt.Request.Files.Add(file);
                }
                else
                {
                    var buffer = new byte[element.Length];
                    stream.Seek(element.Start, SeekOrigin.Begin);
                    stream.Read(buffer, 0, (int) element.Length);

                    if (cnt.Request.Post == null)
                        cnt.Request.Post = new QueryValueCollection();

                    var key = HttpUtility.UrlDecode(element.Name);
                    var val = HttpUtility.UrlDecode(cnt.Request.ContentEncoding.GetString(buffer));

                    cnt.Request.Post.Add(key, val);
                }
            }
        }

        internal static void DecodeUrl(Context pdata, Stream stream)
        {
            pdata.Request.Post = new QueryValueCollection();

            var r = new byte[pdata.Request.ContentLength];
            stream.Read(r, 0, r.Length);
            string str = pdata.Request.ContentEncoding.GetString(r);

            foreach (string s in str.Split('&'))
            {
                string[] val = s.Split(new[] {'='});
                int count = val.Length;
                string k = HttpUtility.UrlDecode(val[0]);

                switch (count)
                {
                    case 2:
                        string v = HttpUtility.UrlDecode(val[1]);
                        pdata.Request.Post.Add(k,v);
                        break;
                    case 1:
                        pdata.Request.Post.Add(k, "true");
                        break;
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
        }

        #region Nested type: HttpMultipart

        internal class HttpMultipart
        {
            private readonly Stream _data;
            private readonly Encoding _encoding;
            private readonly string _boundary;
            private readonly byte[] _boundaryBytes;
            private readonly byte[] buffer;

            private bool _atEof;

            public HttpMultipart(Stream data, string b, Encoding encoding)
            {
                _data = data;
                _boundary = b;
                _boundaryBytes = encoding.GetBytes(b);
                buffer = new byte[_boundaryBytes.Length + 2]; // CRLF or '--'
                _encoding = encoding;
            }

            private static bool CompareBytes(byte[] orig, byte[] other)
            {
                for (int i = orig.Length - 1; i >= 0; i--)
                    if (orig[i] != other[i])
                        return false;

                return true;
            }

            private static string GetContentDispositionAttribute(string l, string name)
            {
                int idx = l.IndexOf(name + "=\"", StringComparison.Ordinal);
                if (idx < 0)
                    return null;
                int begin = idx + name.Length + "=\"".Length;
                int end = l.IndexOf('"', begin);
                if (end < 0)
                    return null;
                if (begin == end)
                    return "";
                return l.Substring(begin, end - begin);
            }

            private string GetContentDispositionAttributeWithEncoding(string l, string name)
            {
                int idx = l.IndexOf(name + "=\"", StringComparison.Ordinal);
                if (idx < 0)
                    return null;
                int begin = idx + name.Length + "=\"".Length;
                int end = l.IndexOf('"', begin);
                if (end < 0)
                    return null;
                if (begin == end)
                    return "";

                string temp = l.Substring(begin, end - begin);
                var source = new byte[temp.Length];
                for (int i = temp.Length - 1; i >= 0; i--)
                    source[i] = (byte) temp[i];

                return _encoding.GetString(source);
            }

            private long MoveToNextBoundary()
            {
                long retval = 0;
                bool gotCr = false;

                int state = 0;
                int c = _data.ReadByte();
                while (true)
                {
                    if (c == -1)
                        return -1;

                    if (state == 0 && c == 0x0A)
                    {
                        retval = _data.Position - 1;
                        if (gotCr)
                            retval--;
                        state = 1;
                        c = _data.ReadByte();
                    }
                    else if (state == 0)
                    {
                        gotCr = (c == 0x0D);
                        c = _data.ReadByte();
                    }
                    else if (state == 1 && c == '-')
                    {
                        c = _data.ReadByte();
                        if (c == -1)
                            return -1;

                        if (c != '-')
                        {
                            state = 0;
                            gotCr = false;
                            continue; // no ReadByte() here
                        }

                        int nread = _data.Read(buffer, 0, buffer.Length);
                        int bl = buffer.Length;
                        if (nread != bl)
                            return -1;

                        if (!CompareBytes(_boundaryBytes, buffer))
                        {
                            state = 0;
                            _data.Position = retval + 2;
                            if (gotCr)
                            {
                                _data.Position++;
                                gotCr = false;
                            }
                            c = _data.ReadByte();
                            continue;
                        }

                        if (buffer[bl - 2] == '-' && buffer[bl - 1] == '-')
                        {
                            _atEof = true;
                        }
                        else if (buffer[bl - 2] != 0x0D || buffer[bl - 1] != 0x0A)
                        {
                            state = 0;
                            _data.Position = retval + 2;
                            if (gotCr)
                            {
                                _data.Position++;
                                gotCr = false;
                            }
                            c = _data.ReadByte();
                            continue;
                        }
                        _data.Position = retval + 2;
                        if (gotCr)
                            _data.Position++;
                        break;
                    }
                    else
                    {
                        // state == 1
                        state = 0; // no ReadByte() here
                    }
                }

                return retval;
            }

            private bool ReadBoundary()
            {
                try
                {
                    string line = ReadLine();
                    while (line == "")
                        line = ReadLine();
                    if (line[0] != '-' || line[1] != '-')
                        return false;

                    if (!EndsWith(line, _boundary))
                        return true;
                }
                catch (Exception)
                {
                }

                return false;
            }

            private static bool EndsWith(string str1, string str2)
            {
                int l2 = str2.Length;
                if (l2 == 0)
                    return true;

                int l1 = str1.Length;
                if (l2 > l1)
                    return false;

                return (0 == String.Compare(str1, l1 - l2, str2, 0, l2, false, CultureInfo.InvariantCulture));
            }


            private string ReadHeaders()
            {
                string s = ReadLine();
                return s == "" ? null : s;
            }

            private string ReadLine()
            {
                var sb = new StringBuilder();
                bool gotCr = false;
                sb.Length = 0;
                while (true)
                {
                    int b = _data.ReadByte();
                    if (b == -1)
                        return null;

                    if (b == 0x0A)
                        break;
                    gotCr = (b == 0x0D);
                    sb.Append((char) b);
                }

                if (gotCr)
                    sb.Length--;

                return sb.ToString();
            }

            private static bool StartsWith(string str1, string str2)
            {
                if (string.IsNullOrEmpty(str2))
                    return true;
                if (str2.Length > str1.Length)
                    return false;

                return (0 == String.Compare(str1, 0, str2, 0, str2.Length, true, CultureInfo.InvariantCulture));
            }

            public Element ReadNextElement()
            {
                if (_atEof || ReadBoundary())
                    return null;

                var elem = new Element();
                string header;
                while ((header = ReadHeaders()) != null)
                {
                    if (StartsWith(header, "Content-Disposition:"))
                    {
                        elem.Name = GetContentDispositionAttribute(header, "name");
                        elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
                    }
                    else if (StartsWith(header, "Content-Type:"))
                    {
                        elem.ContentType = header.Substring("Content-Type:".Length).Trim();
                    }
                }

                var start = _data.Position;
                elem.Start = start;
                var pos = MoveToNextBoundary();
                if (pos == -1)
                    return null;

                elem.Length = pos - start;
                return elem;
            }

            private static string StripPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return "";

                return path.IndexOf(":\\", StringComparison.Ordinal) != 1 && !path.StartsWith("\\\\")
                           ? path
                           : path.Substring(path.LastIndexOf('\\') + 1);
            }

            #region Nested type: Element

            public class Element
            {
                public string ContentType;
                public string Filename;
                public long Length;
                public string Name;
                public long Start;

                public override string ToString()
                {
                    return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
                           Start.ToString(CultureInfo.InvariantCulture) + ", Length " + Length.ToString(CultureInfo.InvariantCulture);
                }
            }

            #endregion
        }

        #endregion
    }
}