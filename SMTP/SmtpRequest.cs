using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;

namespace Netfluid.SMTP
{
    /// <summary>
    /// SMTP client request
    /// </summary>
    public class SmtpRequest
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string UID { get; set; }

        public string IP { get; set; }

        public MailAddress From { get; set; }

        public List<MailAddress> To { get; set; }

        public Stream Stream { get; set; }

        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        readonly string _blobFile;

        internal SmtpRequest(Socket client)
        {
            Stream = new NetworkStream(client);
            reader = new StreamReader(Stream);
            writer = new StreamWriter(Stream);
            IP = client.RemoteEndPoint.ToString();
            To = new List<MailAddress>();

            _blobFile = Path.GetTempFileName();
            UID = Security.UID();
        }

        public Stream Blob
        {
            get { return new FileStream(_blobFile, FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.ReadWrite); }
        }

        public void SaveAs(string path)
        {
            File.Copy(_blobFile,path);
        }

        public void Write(string s)
        {
            writer.Write(s + "\r\n");
            writer.Flush();

            Console.WriteLine(s);
        }

        public string Read()
        {
            try
            {
                var s= reader.ReadLine();
                Console.WriteLine(s);
                return s;
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
