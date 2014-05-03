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

namespace NetFluid
{
    /// <summary>
    ///     Contains all the data recieved from the client
    /// </summary>
    [Serializable]
    public class HttpRequest
    {
        /// <summary>
        ///     Sintactic sugar for Headers["Accept-Types"]
        /// </summary>
        public string[] AcceptTypes;

        /// <summary>
        ///     Encoder detected for request headers
        /// </summary>
        public Encoding ContentEncoding;

        /// <summary>
        ///     Size in bytes of request-body. Zero if method is GET
        /// </summary>
        public long ContentLength;

        /// <summary>
        ///     Coockies recieved from the client
        /// </summary>
        public CookieCollection Cookies;

        /// <summary>
        ///     Files recieved from the client
        /// </summary>
        public HttpFileCollection Files;

        /// <summary>
        ///     Get query values
        /// </summary>
        public QueryValueCollection Get;

        /// <summary>
        ///     Headers of the client request
        /// </summary>
        public WebHeaderCollection Headers;

        /// <summary>
        ///     HTTP method of client request
        /// </summary>
        public string HttpMethod;

        public bool KeepAlive;

        /// <summary>
        ///     Post query values
        /// </summary>
        public QueryValueCollection Post;

        /// <summary>
        ///     From HTTP1.0 to HTTP1.2
        /// </summary>
        public Version ProtocolVersion;

        /// <summary>
        ///     Requested url undecoded
        /// </summary>
        public string RawUrl;

        /// <summary>
        ///     Requested url decoded
        /// </summary>
        public string Url;

        /// <summary>
        ///     Sintatic sugar for Headers["Referrer"]
        /// </summary>
        public string UrlReferrer;

        /// <summary>
        ///     Languages accepted by the client
        /// </summary>
        public string[] UserLanguages;

        /// <summary>
        ///     Merge of Get and post variables
        /// </summary>
        private QueryValueCollection values;

        public HttpRequest()
        {
            Headers = new WebHeaderCollection();
            ProtocolVersion = HttpVersion.Version10;
            ContentEncoding = Encoding.UTF8;
            Cookies = new CookieCollection();
        }

        /// <summary>
        ///     Merge of get and post values
        /// </summary>
        public QueryValueCollection Values
        {
            get
            {
                if (values == null)
                {
                    if (Get != null)
                    {
                        values = new QueryValueCollection(Get);

                        if (Post != null)
                            foreach (QueryValue item in Post)
                            {
                                values.Add(item.Name, item);
                            }
                    }
                    else
                    {
                        values = new QueryValueCollection(Post);
                    }
                }
                return values;
            }
        }

        /// <summary>
        ///     Sintactic sugar for Headers["Content-Type"]
        /// </summary>
        public string ContentType
        {
            get { return Headers["Content-Type"]; }
        }

        /// <summary>
        ///     Sintactic sugar for Headers["UserName-Agent"]
        /// </summary>
        public string UserAgent
        {
            get { return Headers["UserName-Agent"]; }
        }

        /// <summary>
        ///     Sintactic sugar for Headers["Host"]
        /// </summary>
        public string Host
        {
            get { return Headers["Host"]; }
        }
    }
}