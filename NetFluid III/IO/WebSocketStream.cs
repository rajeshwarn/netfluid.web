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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetFluid.IO
{
    internal class WebSocketStream : Stream
    {
        private readonly Stream s;

        public WebSocketStream(Stream s)
        {
            this.s = s;
        }

        public override bool CanRead
        {
            get { return s.CanRead; }
        }

        public override bool CanSeek
        {
            get { return s.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return s.CanWrite; }
        }

        public override long Length
        {
            get { return s.Length; }
        }

        public override long Position
        {
            get { return s.Position; }
            set { s.Position = value; }
        }

        private static byte[] encode(byte[] bytesRaw, int count)
        {
            var bytesFormatted = new List<byte>();
            bytesFormatted.Add(129);

            int indexStartRawData = -1;

            if (count <= 125)
            {
                bytesFormatted.Add((byte) count);
                indexStartRawData = 2;
            }
            else if (count >= 126 && count <= 65535)
            {
                bytesFormatted.AddRange(new[]
                                            {
                                                (byte) 126,
                                                (byte) ((count >> 8) & 255),
                                                (byte) (count & 255)
                                            });
                indexStartRawData = 4;
            }
            else
            {
                bytesFormatted.AddRange(new[]
                                            {
                                                (byte) 127,
                                                (byte) ((count >> 56) & 255),
                                                (byte) ((count >> 48) & 255),
                                                (byte) ((count >> 40) & 255),
                                                (byte) ((count >> 32) & 255),
                                                (byte) ((count >> 24) & 255),
                                                (byte) ((count >> 16) & 255),
                                                (byte) ((count >> 8) & 255),
                                                (byte) (count & 255)
                                            });
                indexStartRawData = 10;
            }
            // put raw data at the correct index
            bytesFormatted.InsertRange(indexStartRawData, bytesRaw.Take(count));
            return bytesFormatted.ToArray();
        }

        private static byte[] decode(byte[] bytes, int count)
        {
            int second = bytes[1] & 127; // AND 0111 1111
            int maskIndex = 2;
            if (second < 126)
            {
                // length fit in second byte
                maskIndex = 2;
            }
            else if (second == 126)
            {
                // next 2 bytes contain length
                maskIndex = 4;
            }
            else if (second == 127)
            {
                // next 8 bytes contain length
                maskIndex = 10;
            }
            // get mask
            byte[] mask = {bytes[maskIndex], bytes[maskIndex + 1], bytes[maskIndex + 2], bytes[maskIndex + 3]};
            int contentIndex = maskIndex + 4;

            // decode
            var decoded = new byte[count - contentIndex];
            for (int i = contentIndex, k = 0; i < count; i++, k++)
            {
                // decoded = byte XOR mask
                decoded[k] = (byte) (bytes[i] ^ mask[k%4]);
            }
            return decoded;
        }

        public override void Flush()
        {
            s.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var b = new byte[count];
            int r = s.Read(b, 0, count);

            if (r == 0)
                return 0;

            byte[] c = decode(b, r);
            Array.Copy(c, 0, buffer, offset, c.Length);
            return c.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return s.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            s.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] b = encode(buffer, count);
            s.Write(b, 0, b.Length);
        }
    }
}