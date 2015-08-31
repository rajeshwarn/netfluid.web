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

namespace Netfluid.Smtp
{
    public class SmtpCapability
    {
        public string Name { get; private set; }
        public string OptionalParameter { get; private set; }

        public SmtpCapability(string name, string optionalParameter = null)
        {
            if (name == null) throw new ArgumentNullException("name");
            
            Name = name;
            OptionalParameter = optionalParameter;
        }

        public override string ToString()
        {
            return Name + (String.IsNullOrEmpty(OptionalParameter) ? "" : " " + OptionalParameter);
        }

        public static readonly SmtpCapability Pipelining = new SmtpCapability("PIPELINING");

        public static SmtpCapability MaxSizePerEmail(long maxBytesPerEmail)
        {
            if (maxBytesPerEmail < 1)
                throw new ArgumentOutOfRangeException("maxBytesPerEmail", "maxBytesPerEmail must be greater than 0");

            return new SmtpCapability("SIZE", maxBytesPerEmail.ToString());
        }
    }
}
