using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace NetFluid.SMTP
{
    /// <summary>
    /// SMTP client
    /// </summary>
    public class Outbound
    {
        public static Exception[] Send(MailMessage message)
        {
            var errors = new List<Exception>();
            var all = message.Bcc.Concat(message.CC.Concat(message.To));

            all.ForEach(x =>
            {
                var mx = Dns.MX(x.Host);

                if (!mx.Any())
                {
                    errors.Add(new SmtpError("no such mx server",x,null));
                    return;
                }

                foreach (var server in mx)
                {
                    try
                    {
                        var smtp = new SmtpClient(server);
                        smtp.Send(message);
                        return;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new SmtpWarning("failed comunication with smtp server "+server,x,ex));
                    }
                }
                errors.Add(new SmtpError("message not send", x, null));
            });
            return errors.ToArray();
        }
    }
}
