// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System;
using System.Net;
using System.Text;
using NetFluid.HTTP;

namespace NetFluid
{
    [Serializable]
    public class HttpResponse
    {
        public Encoding ContentEncoding;
        public string ContentType;
        public CookieCollection Cookies;
        public WebHeaderCollection Headers;
        public bool KeepAlive;
        public Version ProtocolVersion;
        public string StatusDescription;

        private StatusCode status_code;

        public HttpResponse()
        {
            StatusDescription = "OK";
            Headers = new WebHeaderCollection();
            ContentType = "text/html; charset=utf-8";
            ProtocolVersion = HttpVersion.Version11;
            ContentEncoding = Encoding.UTF8;
            StatusCode = StatusCode.Ok;
        }

        public StatusCode StatusCode
        {
            get { return status_code; }
            set
            {
                status_code = value;
                StatusDescription = HttpUtility.GetStatusDescription(value);
            }
        }


        public void Redirect(string url)
        {
            StatusCode = StatusCode.Found; // Found
            Headers.Append("Location", url);
        }
    }
}