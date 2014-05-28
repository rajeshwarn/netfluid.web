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
    /// <summary>
    /// HTTP response  to sent to the cleint
    /// </summary>
    [Serializable]
    public class HttpResponse
    {
        /// <summary>
        /// Content encoding used by Reader and Writer.By default is instanciated from the client request
        /// </summary>
        public Encoding ContentEncoding;

        /// <summary>
        /// Response mime type. Default value: text/html
        /// </summary>
        public string ContentType;

        /// <summary>
        /// Cookies to the client
        /// </summary>
        public CookieCollection Cookies;
        
        /// <summary>
        /// Headers to the client
        /// </summary>
        public WebHeaderCollection Headers;

        /// <summary>
        /// True if Keep Alive flag was asked by the client
        /// </summary>
        public bool KeepAlive;

        /// <summary>
        /// HTTP protocol version. Default value 1.2
        /// </summary>
        public Version ProtocolVersion;

        /// <summary>
        /// Status code text message. Default value: OK
        /// </summary>
        public string StatusDescription;

        private StatusCode status_code;

        public HttpResponse() 
        {
            StatusDescription = "OK";

            Headers = new WebHeaderCollection();
            Headers.Set("Server", "NetFluid III");
            Headers.Set("Date", DateTime.Now.ToGMT());
            Headers.Set("Accept-Ranges:","bytes");
            Headers.Set("Vary","Accept-Encoding");
            Headers.Set("Access-Control-Allow-Origin", "*");

            ContentType = "text/html; charset=utf-8";
            ProtocolVersion = HttpVersion.Version11;
            ContentEncoding = Encoding.UTF8;
            StatusCode = StatusCode.Ok;
        }

        /// <summary>
        /// HTTP status code.Default value:200
        /// </summary>
        public StatusCode StatusCode
        {
            get { return status_code; }
            set
            {
                status_code = value;
                StatusDescription = HttpUtility.GetStatusDescription(value);
            }
        }

        /// <summary>
        /// Redirect the client to a different URI or URL
        /// </summary>
        /// <param name="url"></param>
        public void Redirect(string url)
        {
            StatusCode = StatusCode.Found; // Found
            Headers.Append("Location", url);
        }

        /// <summary>
        /// Redirect permanently the client to a different URI o URL
        /// </summary>
        /// <param name="url"></param>
        public void MovedPermanently(string url)
        {
            StatusCode = StatusCode.MovedPermanently;
            Headers.Append("Location", url);
        }
    }
}