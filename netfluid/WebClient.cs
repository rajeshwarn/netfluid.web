using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace NetFluid
{
    public class WebClient
    {
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
