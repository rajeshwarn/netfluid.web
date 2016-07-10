using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Netfluid.Dns.Records;

namespace Netfluid.Dns
{
    class Serializer
    {
        #region DESERIALIZE

        private static byte ReadByte(Stream stream)
        {
            return (byte) stream.ReadByte();
        }

        private static ushort ReadUInt16(Stream stream)
        {
            return (ushort) (stream.ReadByte() << 8 | stream.ReadByte());
        }

        private static uint ReadUInt32(Stream ms)
        {
            uint res;

            if (BitConverter.IsLittleEndian)
            {
                res = (((uint) ReadByte(ms) << 24) | ((uint) ReadByte(ms) << 16) |
                       ((uint) ReadByte(ms) << 8) | ReadByte(ms));
            }
            else
            {
                res = (ReadByte(ms) | ((uint) ReadByte(ms) << 8) |
                       ((uint) ReadByte(ms) << 16) | ((uint) ReadByte(ms) << 24));
            }

            return res;
        }

        private static int ReadInt32(Stream ms)
        {
            int res;

            if (BitConverter.IsLittleEndian)
            {
                res = ((ReadByte(ms) << 24) | (ReadByte(ms) << 16) |
                       (ReadByte(ms) << 8) | ReadByte(ms));
            }
            else
            {
                res = (ReadByte(ms) | (ReadByte(ms) << 8) |
                       (ReadByte(ms) << 16) | (ReadByte(ms) << 24));
            }

            return res;
        }

        private static byte[] ReadByteArray(Stream ms, int size)
        {
            var value = new byte[size];
            ms.Read(value, 0, value.Length);
            return value;
        }

        private static string ReadText(Stream ms)
        {
            int s = ms.ReadByte();
            var b = new byte[s];
            ms.Read(b, 0, b.Length);
            return Encoding.ASCII.GetString(b);
        }

        private static void ParseDomainName(byte[] resultData, ref int currentPosition, StringBuilder sb)
        {
            while (true)
            {
                if (currentPosition >= resultData.Length)
                    return;

                byte currentByte = resultData[currentPosition++];
                if (currentByte == 0)
                {
                    // end of domain, RFC1035
                    return;
                }
                if (currentByte >= 192)
                {
                    // Pointer, RFC1035
                    int pointer;
                    if (BitConverter.IsLittleEndian)
                    {
                        pointer = (ushort)(((currentByte - 192) << 8) | resultData[currentPosition++]);
                    }
                    else
                    {
                        pointer = (ushort)((currentByte - 192) | (resultData[currentPosition++] << 8));
                    }

                    ParseDomainName(resultData, ref pointer, sb);

                    return;
                }
                if (currentByte == 65)
                {
                    // binary EDNS label, RFC2673, RFC3363, RFC3364
                    int length = resultData[currentPosition++];
                    if (length == 0)
                        length = 256;

                    sb.Append(@"\[x");
                    string suffix = "/" + length + "]";

                    do
                    {
                        currentByte = resultData[currentPosition++];
                        if (length < 8)
                        {
                            currentByte &= (byte)(0xff >> (8 - length));
                        }

                        sb.Append(currentByte.ToString("x2"));

                        length = length - 8;
                    } while (length > 0);

                    sb.Append(suffix);
                }
                else if (currentByte >= 64)
                {
                    // extended dns label RFC 2671
                    //var kk = resultData.Skip(currentPosition + 1).ToArray();
                    //sb.Append(Encoding.UTF8.GetString(kk));
                    return;
                    //Console.WriteLine();
                    //throw new NotSupportedException("Unsupported extended dns label");
                }
                else
                {
                    // append additional text part
                    sb.Append(Encoding.ASCII.GetString(resultData, currentPosition, currentByte));
                    sb.Append(".");
                    currentPosition += currentByte;
                }
            }
        }

        private static string ReadDomainName(MemoryStream stream)
        {
            var sb = new StringBuilder();
            var pos = (int)stream.Position;

            ParseDomainName(stream.ToArray(), ref pos, sb);
            stream.Position = pos;
            return (sb.Length == 0) ? String.Empty : sb.ToString(0, sb.Length - 1);
        }

        private static Header ReadHeader(Stream stream)
        {
            return new Header
            {
                ID = ReadUInt16(stream),
                Flags = ReadUInt16(stream),
                QDCOUNT = ReadUInt16(stream),
                ANCOUNT = ReadUInt16(stream),
                NSCOUNT = ReadUInt16(stream),
                ARCOUNT = ReadUInt16(stream)
            };
        }

        private static Question ReadQuestion(MemoryStream stream)
        {
            return new Question(ReadDomainName(stream), (QType) ReadUInt16(stream), (QClass) ReadUInt16(stream));
        }

        public static Request ReadRequest(MemoryStream stream)
        {
            var req = new Request {Header = ReadHeader(stream)};

            for (int i = 0; i < req.Header.QDCOUNT; i++)
            {
                req.Add(ReadQuestion(stream));
            }
            return req;
        }

        private static Record ReadRecord(MemoryStream ms)
        {
            const int timeLived = 0;
            string name = ReadDomainName(ms);
            var recordType = (RecordType) ReadUInt16(ms);
            var @class = (Class) ReadUInt16(ms);
            uint ttl = ReadUInt32(ms);
            ushort rdlength = ReadUInt16(ms);

            var record = Record.FromType(recordType);
            Type type = record.GetType();

            int length = rdlength;

            record.TimeLived = timeLived;
            record.Name = name;
            record.Class = @class;
            record.TTL = ttl;

            long nspos = ms.Position + rdlength;

            foreach (FieldInfo field in type.GetFields().Where(x => x.DeclaringType == type))
            {
                Type fieldType = field.FieldType;

                if (fieldType == typeof (byte))
                {
                    field.SetValue(record, (byte) ms.ReadByte());
                }
                else if (fieldType == typeof (ushort))
                {
                    field.SetValue(record, ReadUInt16(ms));
                }
                else if (fieldType == typeof (int))
                {
                    field.SetValue(record, ReadInt32(ms));
                }
                else if (fieldType == typeof (uint))
                {
                    field.SetValue(record, ReadUInt32(ms));
                }
                else if (fieldType == typeof (byte[]))
                {
                    field.SetValue(record, ReadByteArray(ms, length));
                }
                else if (fieldType == typeof (string))
                {
                    if (field.HasAttribute<DomainNameAttribute>())
                        field.SetValue(record, ReadDomainName(ms));
                    else
                        field.SetValue(record, ReadText(ms));
                }
            }

            if (nspos > ms.Position)
            {
                ms.Position = nspos;
            }
            return record;
        }

        public static Response ReadResponse(byte[] data)
        {
            var ms = new MemoryStream(data);

            var r = new Response
            {
                MessageSize = data.Length,
                Questions = new List<Question>(),
                Answers = new List<Record>(),
                Authorities = new List<Record>(),
                Additionals = new List<Record>(),
                Header = ReadHeader(ms)
            };


            for (int intI = 0; intI < r.Header.QDCOUNT; intI++)
            {
                r.Questions.Add(ReadQuestion(ms));
            }

            for (int intI = 0; intI < r.Header.ANCOUNT; intI++)
            {
                r.Answers.Add(ReadRecord(ms));
            }

            for (int intI = 0; intI < r.Header.NSCOUNT; intI++)
            {
                r.Authorities.Add(ReadRecord(ms));
            }
            for (int intI = 0; intI < r.Header.ARCOUNT; intI++)
            {
                r.Additionals.Add(ReadRecord(ms));
            }

            return r;
        }

        #endregion

        private static void WriteUInt16(Stream ms, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ms.WriteByte((byte) ((value >> 8) & 0xff));
                ms.WriteByte((byte) (value & 0xff));
            }
            else
            {
                ms.WriteByte((byte) (value & 0xff));
                ms.WriteByte((byte) ((value >> 8) & 0xff));
            }
        }

        private static void WriteInt32(Stream ms, int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ms.WriteByte((byte) ((value >> 24) & 0xff));
                ms.WriteByte((byte) ((value >> 16) & 0xff));
                ms.WriteByte((byte) ((value >> 8) & 0xff));
                ms.WriteByte((byte) (value & 0xff));
            }
            else
            {
                ms.WriteByte((byte) (value & 0xff));
                ms.WriteByte((byte) ((value >> 8) & 0xff));
                ms.WriteByte((byte) ((value >> 16) & 0xff));
                ms.WriteByte((byte) ((value >> 24) & 0xff));
            }
        }

        private static void WriteUInt32(Stream ms, uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ms.WriteByte((byte) ((value >> 24) & 0xff));
                ms.WriteByte((byte) ((value >> 16) & 0xff));
                ms.WriteByte((byte) ((value >> 8) & 0xff));
                ms.WriteByte((byte) (value & 0xff));
            }
            else
            {
                ms.WriteByte((byte) (value & 0xff));
                ms.WriteByte((byte) ((value >> 8) & 0xff));
                ms.WriteByte((byte) ((value >> 16) & 0xff));
                ms.WriteByte((byte) ((value >> 24) & 0xff));
            }
        }

        private static void WriteByteArray(Stream ms, byte[] value)
        {
            ms.Write(value, 0, value.Length);
        }

        private static void WriteDomainName(Stream ms, string name)
        {
            while (true)
            {
                if (String.IsNullOrEmpty(name) || (name == "."))
                {
                    ms.WriteByte(0);
                    return;
                }

                int labelLength = name.IndexOf('.');
                if (labelLength == -1)
                    labelLength = name.Length;

                ms.WriteByte((byte) labelLength);
                WriteByteArray(ms, Encoding.ASCII.GetBytes(name.ToCharArray(0, labelLength)));

                name = labelLength == name.Length ? "." : name.Substring(labelLength + 1);
            }
        }

        public static void WriteText(Stream ms, string name)
        {
            byte[] d = Encoding.ASCII.GetBytes(name);
            ms.Write(d, 0, d.Length);
        }

        private static void WriteHeader(Stream ms, Header header)
        {
            WriteUInt16(ms, header.ID);
            WriteUInt16(ms, header.Flags);
            WriteUInt16(ms, header.QDCOUNT);
            WriteUInt16(ms, header.ANCOUNT);
            WriteUInt16(ms, header.NSCOUNT);
            WriteUInt16(ms, header.ARCOUNT);
        }

        public static void WriteQuestion(Stream ms, Question q)
        {
            WriteDomainName(ms, q.QName);
            WriteUInt16(ms, (ushort) q.QType);
            WriteUInt16(ms, (ushort) q.QClass);
        }

        private static void WriteRecord(Stream ms, Record rr)
        {
            WriteDomainName(ms, rr.Name);
            WriteUInt16(ms, (ushort) Enum.Parse(typeof (RecordType), rr.GetType().Name.Substring("Record".Length)));
            WriteUInt16(ms, (ushort) rr.Class);
            WriteUInt32(ms, rr.TTL);

            var n = new MemoryStream();
            WriteRecordData(n, rr);
            WriteUInt16(ms, (ushort) n.Length);
            WriteByteArray(ms, n.ToArray());
        }

        private static void WriteRecordData(Stream ms, Record record)
        {
            var type = record.GetType();
            foreach (var field in type.GetFields().Where(x => x.DeclaringType == type))
            {
                var fieldType = field.FieldType;
                var value = field.GetValue(record);

                if (fieldType == typeof (byte))
                    ms.WriteByte((byte) value);
                else if (fieldType == typeof (ushort))
                    WriteUInt16(ms, (ushort) value);
                else if (fieldType == typeof (int))
                    WriteInt32(ms, (int) value);
                else if (fieldType == typeof (uint))
                    WriteUInt32(ms, (uint) value);
                else if (fieldType == typeof (byte[]))
                    WriteByteArray(ms, (byte[]) value);
                else if (fieldType == typeof (string))
                {
                    if (field.HasAttribute<DomainNameAttribute>())
                        WriteDomainName(ms, (string) value);
                    else
                        WriteText(ms, (string) value);
                }
            }
        }

        public static byte[] WriteResponse(Response response)
        {
            var ms = new MemoryStream();

            response.Header.QDCOUNT = (ushort) response.Questions.Count;
            response.Header.ANCOUNT = (ushort) response.Answers.Count;
            response.Header.NSCOUNT = (ushort) response.Authorities.Count;
            response.Header.ARCOUNT = (ushort) response.Additionals.Count;

            WriteHeader(ms, response.Header);

            response.Questions.ForEach(x => WriteQuestion(ms, x));
            response.Answers.ForEach(x => WriteRecord(ms, x));
            response.Authorities.ForEach(x => WriteRecord(ms, x));
            response.Additionals.ForEach(x => WriteRecord(ms, x));

            return ms.ToArray();
        }
    }
}