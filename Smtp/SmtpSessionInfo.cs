﻿#region Header
// Copyright (c) 2013-2015 Hans Wolff
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;

namespace Netfluid.Smtp
{
    public class SmtpSessionInfo
    {
        public Guid Guid { get; private set; }
        public bool HasData { get; set; }
        public SmtpIdentification Identification { get; set; }
        public MailAddress MailFrom { get; set; }
        public List<MailAddress> Recipients { get; private set; }
        public object Tag { get; set; }

        public DateTime CreatedTimestamp { get; private set; }

        public Stream DataStream { get; private set; }

        public SmtpSessionInfo()
        {
            CreatedTimestamp = DateTime.UtcNow;

            Identification = new SmtpIdentification();
            Recipients = new List<MailAddress>();
            Guid = Guid.NewGuid();

            DataStream = new FileStream(Path.Combine(Path.GetTempPath(), Guid.ToString()),FileMode.OpenOrCreate);
        }

        public void Reset()
        {
            HasData = false;
            MailFrom = null;
            Recipients.Clear();
        }
    }
}
