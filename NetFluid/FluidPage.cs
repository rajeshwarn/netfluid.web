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

using NetFluid.HTTP;

namespace NetFluid
{
    public class FluidPage : IMethodExposer
    {
        public FluidPage()
        {
        }

        public FluidPage(Context cnt)
        {
            Context = cnt;
        }

        public HttpResponse Response
        {
            get { return Context.Response; }
        }

        public HttpRequest Request
        {
            get { return Context.Request; }
        }

        public QueryValueCollection Get
        {
            get { return Context.Request.Get ?? (Context.Request.Get = new QueryValueCollection()); }
        }

        public QueryValueCollection Post
        {
            get
            {
                if (Context.Request.Post == null)
                    Context.Request.Post = new QueryValueCollection();
                return Context.Request.Post;
            }
        }

        public HttpFileCollection Files
        {
            get { return Context.Request.Files ?? (Context.Request.Files = new HttpFileCollection()); }
        }

        public Context Context { get; set; }

        public virtual object Run()
        {
            return null;
        }

        public void Session(string name, object obj)
        {
            Context.Session(name, obj);
        }

        public T Session<T>(string name)
        {
            return Context.Session<T>(name);
        }

        public dynamic Session(string name)
        {
            return Context.Session(name);
        }

        public void SendRaw(byte[] bytes)
        {
            Context.SendHeaders();
            Context.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public string URLEncode(string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        public string URLDecode(string str)
        {
            return HttpUtility.UrlDecode(str);
        }

        public string HTMLEncode(string str)
        {
            return HttpUtility.HtmlEncode(str);
        }

        public string HTMLDecode(string str)
        {
            return HttpUtility.HtmlDecode(str);
        }
    }
}