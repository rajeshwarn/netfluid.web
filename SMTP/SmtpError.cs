using System;
using System.Net.Mail;

namespace Netfluid.SMTP
{
    /// <summary>
    /// SMTP protocol violation
    /// </summary>
    public class SmtpError : Exception
    {
        public MailAddress Address;

        public SmtpError(string message, MailAddress address, Exception inner)
            : base(message, inner)
        {
            Address = address;
        }
    }
}
