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
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetFluid
{
    public class MethodExposer:IDisposable
    {
        public MethodExposer()
        {
        }

        public MethodExposer(Context cnt)
        {
            Context = cnt;
        }
        
        /// <summary>
        /// Syntactic sugar for Context.Response
        /// </summary>
        public HttpListenerResponse Response
        {
            get { return Context.Response; }
        }

        /// <summary>
        /// Syntactic sugar for Context.Request
        /// </summary>
        public HttpListenerRequest Request
        {
            get { return Context.Request; }
        }

        /// <summary>
        /// Syntactic sugar for Context.Request.Files
        /// </summary>
        public HttpFileCollection Files
        {
            get { return Context.Files; }
        }

        /// <summary>
        /// Used by the Engine to excute exposed methods
        /// </summary>
        public Context Context { get; set; }

        /// <summary>
        /// The host where the exposer is in
        /// </summary>
        public Host Host { get; set; }


        /// <summary>
        /// Invoked when any host has been loaded and the server start to listen for clients
        /// </summary>
        public virtual void OnServerStart()
        {

        }

        /// <summary>
        /// Return the path based of this assembly location
        /// </summary>
        /// <returns></returns>
        public string RelativePath(string path)
        {
            var type = this.GetType();
            var assembly = type.Assembly;

            if (string.IsNullOrEmpty(assembly.Location))
            {
                throw new Exception("Virtual assembly");
            }

            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assembly.Location),path));
        }

        /// <summary>
        /// Set a session variable, if the session does not exits is built
        /// </summary>
        /// <param name="name">name of the variable</param>
        /// <param name="obj">value</param>
        public void Session(string name, object obj)
        {
            Host.Sessions.Set(Context.SessionId, name, obj);
        }

        /// <summary>
        /// Retrieve a value from session, default(T) if does not exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">name of the variable</param>
        /// <returns></returns>
        public T Session<T>(string name)
        {
            return (T)Host.Sessions.Get(Context.SessionId, name);
        }

        /// <summary>
        /// Retrieve a value from session, default(T) if does not exists
        /// </summary>
        /// <param name="name">name of the variable</param>
        /// <returns></returns>
        public dynamic Session(string name)
        {
            return Host.Sessions.Get(Context.SessionId, name);
        }

        /// <summary>
        /// Remove a single value from the session
        /// </summary>
        /// <param name="name"></param>
        public void SessionRemove(string name)
        {
            Host.Sessions.Remove(Context.SessionId,name);
        }

        /// <summary>
        /// Remove the whole current session
        /// </summary>
        public void SessionDestroy()
        {
            Host.Sessions.Destroy(Context.SessionId);
        }

        /// <summary>
        /// Send Response  headers to the client and write byte directly to the client (via OutputStream)
        /// </summary>
        public void SendRaw(byte[] bytes)
        {
            Context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        #region remotes

        public static T GetRemoteObject<T>(string uri, T toBeFilled)
        {
            return GetRemoteObject(new Uri(uri), toBeFilled);
        }

        public static T GetRemoteObject<T>(Uri uri, T toBeFilled)
        {
            //FIXME
            /*
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Web Application";
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 1000;

            WebResponse response = request.GetResponse();
            var liner = new StreamReader(response.GetResponseStream());

            return Newtonsoft.Json.JsonConvert.DeserializeAnonymousType<T>(liner.ReadToEnd(), toBeFilled);*/
            return default(T);
        }

        public static dynamic GetRemoteObject(Uri uri)
        {
            /*
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Web Application";
            request.KeepAlive = false;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 1000;

            WebResponse response = request.GetResponse();
            var liner = new StreamReader(response.GetResponseStream());
            var json = liner.ReadToEnd();
            
            return Parse(Newtonsoft.Json.JsonConvert.DeserializeObject(json));*/
            return null;
        }

        static dynamic Parse(object obj)
        {
            //FIXME
            /*
            if (obj is Newtonsoft.Json.Linq.JArray)
            {
                var list = new List<object>();
                var a = obj as Newtonsoft.Json.Linq.JArray;
                a.ForEach(x => list.Add(Parse(x)));
                return list;
            }
            if(obj is Newtonsoft.Json.Linq.JObject)
            {
                var o = obj as Newtonsoft.Json.Linq.JObject;
                var ex = new ExpandoObject();

                foreach (var item in o.Properties())
                {
                    o[item.Name] = Parse(item.Value);
                }
                return o;
            }
            if(obj is Newtonsoft.Json.Linq.JValue)
            {
                obj.ToString();
            }*/
            return null;
        }

        public static T GetRemoteObject<T>(Uri uri)
        {
            //FIXME
            /*
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Web Application";
            request.KeepAlive = false;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 1000;

            WebResponse response = request.GetResponse();
            var liner = new StreamReader(response.GetResponseStream());
            var json = liner.ReadToEnd();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
             */
            return default(T);
        }

        /// <summary>
        ///     Download specified uri as a stream
        /// </summary>
        /// <param name="uri">uri to download</param>
        /// <param name="accept">comma separated accepted mime types</param>
        /// <returns></returns>
        public static Stream GetRemoteStream(Uri uri, string accept = "text/html, text/plain")
        {
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Web Application";
            request.Accept = accept;
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 10000;

            WebResponse response = request.GetResponse();
            return response.GetResponseStream();
        }

        /// <summary>
        ///     Download specified uri as lines of text
        /// </summary>
        /// <param name="uri">uri to download</param>
        /// <param name="accept">comma separated accepted mime types</param>
        /// <returns></returns>
        public static IEnumerable<string> GetRemoteLines(Uri uri, string accept = "text/html, text/plain")
        {
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Web Application";
            request.Accept = accept;
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 10000;

            WebResponse response = request.GetResponse();
            var liner = new StreamReader(response.GetResponseStream());

            while (!liner.EndOfStream)
            {
                yield return liner.ReadLine();
            }
        }

        /// <summary>
        ///     Download specified as a string
        /// </summary>
        /// <param name="uri">uri to download</param>
        /// <param name="accept">comma separated accepted mime types</param>
        /// <returns></returns>
        public static string GetRemoteString(Uri uri, string accept = "text/html, text/plain")
        {
            var request = WebRequest.Create(uri) as HttpWebRequest;
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Web Application";
            request.Accept = accept;
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 10000;

            WebResponse response = request.GetResponse();
            var liner = new StreamReader(response.GetResponseStream());
            return liner.ReadToEnd();
        }
        #endregion

        #region encoding
        /// <summary>
        /// URL encode (http://www.w3schools.com/tags/ref_urlencode.asp)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string URLEncode(string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        /// <summary>
        /// URL decode (http://www.w3schools.com/tags/ref_urlencode.asp)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
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

        #endregion

        #region Binary
        /// <summary>
        /// Serialize the object to the stream
        /// </summary>
        /// <param name="obj">object to serialize</param>
        /// <param name="s">target stream</param>
        public static void ToBinary(object obj, Stream s)
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(s, obj);
        }

        /// <summary>
        /// Binary serialize an object
        /// </summary>
        /// <param name="obj">object to be serialized</param>
        /// <returns></returns>
        public static byte[] ToBinary(object obj)
        {
            var formatter = new BinaryFormatter();
            var s = new MemoryStream();
            formatter.Serialize(s, obj);
            return s.ToArray();
        }

        /// <summary>
        /// Deserialize an object form the stream
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="s">reading stream</param>
        /// <returns>deserialized T object</returns>
        public static T FromBinary<T>(Stream s)
        {
            var formatter = new BinaryFormatter();
            object dbg = formatter.Deserialize(s);
            return (T)dbg;
        }

        /// <summary>
        /// Deserialize an object form the bynary array
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="b">binary serialized value</param>
        /// <returns>deserialized T object</returns>
        public static T FromBinary<T>(byte[] b)
        {
            var formatter = new BinaryFormatter();
            var s = new MemoryStream();
            s.Write(b, 0, b.Length);
            s.Seek(0, SeekOrigin.Begin);
            object dbg = formatter.Deserialize(s);
            return (T)dbg;
        }

        /// <summary>
        /// Deserialize a binary serialized object
        /// </summary>
        /// <param name="b">binary serialized value</param>
        /// <param name="type">object target type</param>
        /// <returns>deserialized object</returns>
        public static object FromBinary(byte[] b, Type type)
        {
            var formatter = new BinaryFormatter();
            var s = new MemoryStream();
            s.Write(b, 0, b.Length);
            s.Seek(0, SeekOrigin.Begin);
            object dbg = formatter.Deserialize(s);
            return dbg;
        }
        #endregion

        #region JSON
        public static string ToJSON(object obj)
        {
            //FIXME
           // return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return "";
        }

        public static dynamic FromJSON(string json)
        {
           // return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            return null;
        }
        #endregion

        public void Dispose()
        {
        }
    }
}