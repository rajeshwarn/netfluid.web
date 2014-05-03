using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using MimeKit;

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
            var mime = MimeMessage.Load(_blobFile);
            var message = new MailMessage();

            mime.Bcc.ForEach(x=>message.Bcc.Add(new MailAddress(x.Name)));
            mime.Cc.ForEach(x => message.CC.Add(new MailAddress(x.Name)));
            message.From = new MailAddress(mime.From[0].Name);
            mime.ReplyTo.ForEach(x=>message.ReplyToList.Add(new MailAddress(x.Name)));
            message.Subject = mime.Subject;
            mime.To.ForEach(x => message.To.Add(new MailAddress(x.Name)));

            RenderMimeEntity(mime.Body,message);
            return message;
        }

        private static void RenderMimeEntity(MimeEntity entity, MailMessage message)
        {
            while (true)
            {
                var messagePart = entity as MessagePart;
                if (messagePart != null)
                {
                    // If you'd like to render this inline instead of treating
                    // it as an attachment, you would just continue to recurse:
                    entity = messagePart.Message.Body;
                    continue;
                }
                var multipart  = entity as Multipart;
                if (multipart != null)
                {
                    // This entity is a multipart container.

                    foreach (var subpart in multipart)
                        RenderMimeEntity(subpart, message);
                }
                else
                {
                    // Everything that isn't either a MessagePart or a Multipart is a MimePart
                    var part = (MimePart) entity;

                    // Don't render anything that is explicitly marked as an attachment.
                    if (part.IsAttachment || entity.ContentType.Matches("image", "*"))
                    {
                        var fs = new FileStream(Security.TempFile, FileMode.OpenOrCreate);
                        part.ContentObject.DecodeTo(fs);
                        fs.Flush(true);
                        fs.Seek(0, SeekOrigin.Begin);
                        var att = new Attachment(fs, part.FileName);
                        message.Attachments.Add(att);
                    }

                    var text = part as TextPart;
                    if (text != null)
                    {
                        message.Body = text.Text;
                        message.IsBodyHtml = text.ContentType.Matches("text", "html");
                    }
                }
                break;
            }
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
