using NetFluid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluidService
{
    public class SiteManager:FluidPage
    {
        static SiteManager()
        {
            if (!Directory.Exists("./updates"))
            {
                Directory.CreateDirectory("./updates");
            }
            if (!Directory.Exists("./docs"))
            {
                Directory.CreateDirectory("./docs");
            }
        }
        
        [Route("/")]
        public object Index()
        {
            return new FluidTemplate("index.html", "myphoto.jpg", "matteo", "fabbri");
        }

        [CallOn(404)]
        public void Re()
        {
            this.Response.MovedPermanently("/");
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
            yield return ("</urlset>");
        }

    }
}
