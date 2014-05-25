using System;
using System.Net.Mail;

namespace NetFluid.SMTP
{
    public class SmtpWarning : Exception
    {
        public MailAddress Address;

        public SmtpWarning(string message,MailAddress address, Exception inner):base(message,inner)
        {
            Address = address;
        }
    }
}
