using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluidService
{
    public class ServiceManager:FluidPage
    {
        [CallOn(404)]
        [Route("/")]
        public object Index()
        {
            return new FluidTemplate("./UI/index.html");
        }

        [Route("/robots.txt")]
        public string Robot()
        { 
            return "Sitemap: http://netfluid.org/sitemap.xml\r\n";
        }


        [Route("/sitemap.xml")]
        public IEnumerable<string> Sitemap()
        {
            Response.ContentType = "application/xml";
            yield return ("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            yield return ("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            var d = DateTime.Now.ToString("yyyy-MM-dd");
            yield return ("<url><loc>http://netfluid.org</loc><lastmod>" + d + "</lastmod><changefreq>always</changefreq><priority>0.8</priority></url>");
            yield return ("<url><loc>http://netfluid.org/help</loc><lastmod>" + d + "</lastmod><changefreq>daily</changefreq><priority>0.8</priority></url>");
            yield return ("<url><loc>http://netfluid.org/examples</loc><lastmod>" + d + "</lastmod><changefreq>daily</changefreq><priority>0.8</priority></url>");
            yield return ("<url><loc>http://netfluid.org/documentation</loc><lastmod>" + d + "</lastmod><changefreq>daily</changefreq><priority>0.8</priority></url>");
            yield return ("<url><loc>http://netfluid.org/discover</loc><lastmod>" + d + "</lastmod><changefreq>daily</changefreq><priority>0.8</priority></url>");
            yield return ("<url><loc>http://netfluid.org/download</loc><lastmod>" + d + "</lastmod><changefreq>daily</changefreq><priority>0.8</priority></url>");
            foreach (var art in Article.All())
            {
                yield return (string.Format("<url><loc>http://netfluid.org/read/{0}</loc><lastmod>{1}</lastmod><changefreq>monthly</changefreq><priority>0.8</priority></url>", art.Link, art.DateTime.ToString("yyyy-MM-dd")));
            }


            yield return ("</urlset>");
        }

    }
}
