using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NetFluid
{
    public class WebClient
    {
        /// <summary>
        /// Download specified as a stream
        /// </summary>
        /// <param name="uri">uri to download</param>
        /// <param name="accept">comma separated accepted mime types</param>
        /// <returns></returns>
        public static Stream GetStream(Uri uri, string accept = "text/html, text/plain")
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Scraper Agent";
            request.Accept = accept;
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 10000;

            var response = request.GetResponse();
            return response.GetResponseStream();
        }

        /// <summary>
        /// Download specified as lines of text
        /// </summary>
        /// <param name="uri">uri to download</param>
        /// <param name="accept">comma separated accepted mime types</param>
        /// <returns></returns>
        public static IEnumerable<string> GetLines(Uri uri, string accept = "text/html, text/plain")
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Scraper Agent";
            request.Accept = accept;
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 10000;

            var response = request.GetResponse();
            var liner= new StreamReader(response.GetResponseStream());

            while (!liner.EndOfStream)
            {
                yield return liner.ReadLine();
            }
        }

        /// <summary>
        /// Download specified as a string
        /// </summary>
        /// <param name="uri">uri to download</param>
        /// <param name="accept">comma separated accepted mime types</param>
        /// <returns></returns>
        public static string GetString(Uri uri, string accept = "text/html, text/plain")
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = true;
            request.UserAgent = "NetFluid Scraper Agent";
            request.Accept = accept;
            request.KeepAlive = true;
            request.Proxy = null;
            request.MaximumAutomaticRedirections = 10;
            request.AutomaticDecompression = DecompressionMethods.None;
            request.Timeout = 10000;

            var response = request.GetResponse();
            var liner = new StreamReader(response.GetResponseStream());
            return liner.ReadToEnd();
        }
    }
}
