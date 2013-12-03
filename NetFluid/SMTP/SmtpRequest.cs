using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;

namespace NetFluid.SMTP
{
    public class SmtpRequest
    {
        public string UID;
        public string IP;
        public MailAddress From;
        public List<MailAddress> To;

        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        public string BlobFile;

        public SmtpRequest(Socket client)
        {
            var stream = new NetworkStream(client);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            IP = client.RemoteEndPoint.ToString();
            To = new List<MailAddress>();

            BlobFile = Path.GetTempFileName();
            UID = NetFluid.Security.UID();
        }

        public Stream Blob
        {
            get { return new FileStream(BlobFile, FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.ReadWrite); }
        }

        public void SaveAs(string path)
        {
            File.Copy(BlobFile,path);
        }

        public void Write(string s)
        {
            writer.Write(s + "\r\n");
            writer.Flush();
        }

        public string Read()
        {
            try
            {
                return reader.ReadLine();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public void Reset()
        {
            To.Clear();
        }
    }
}
