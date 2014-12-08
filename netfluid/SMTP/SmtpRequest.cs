using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Sockets;

namespace NetFluid.SMTP
{
    /// <summary>
    /// SMTP client request
    /// </summary>
    public class SmtpRequest
    {
        public string UID;
        public string IP;
        public MailAddress From;
        public List<MailAddress> To;

        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        readonly string _blobFile;

        internal SmtpRequest(Socket client)
        {
            var stream = new NetworkStream(client);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            IP = client.RemoteEndPoint.ToString();
            To = new List<MailAddress>();

            _blobFile = Path.GetTempFileName();
            UID = Security.UID();
        }

        public MailMessage Parse()
        {
            /*var mime = MimeMessage.Load(_blobFile);
            var message = new MailMessage();

            mime.Bcc.ForEach(x=>message.Bcc.Add(new MailAddress(x.Name)));
            mime.Cc.ForEach(x => message.CC.Add(new MailAddress(x.Name)));
            message.From = new MailAddress(mime.From[0].Name);
            mime.ReplyTo.ForEach(x=>message.ReplyToList.Add(new MailAddress(x.Name)));
            message.Subject = mime.Subject;
            mime.To.ForEach(x => message.To.Add(new MailAddress(x.Name)));

            RenderMimeEntity(mime.Body,message);
            return message;*/
            return null;
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
