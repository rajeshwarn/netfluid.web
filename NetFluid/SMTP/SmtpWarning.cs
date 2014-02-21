using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

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
