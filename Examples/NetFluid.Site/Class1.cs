using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetFluid.Site
{
    public class Class1:FluidPage
    {
        string[] Docs { get { return Directory.GetFiles("./docs"); } }

        [Route("/")]
        public IResponse Home()
        {
            return new FluidTemplate("./UI/Home.html",Docs.Select(Path.GetFileNameWithoutExtension));
        }

        [ParametrizedRoute("/read")]
        public IResponse Read(string article)
        {
            var p = Path.GetFullPath("./docs/" + article + ".html");

            if (File.Exists(p) && p.StartsWith(Path.GetFullPath("./docs")))
            {
                return new FluidTemplate("./UI/Read.html",article, File.ReadAllText(p));
            }
            return new FluidTemplate("./UI/Home.html");
        }

        [Route("robots.txt")]
        public string Robot()
        {
            return "Sitemap: http://netfluid.org/sitemap.xml";
        }

        [Route("/sitemap.xml")]
        public IEnumerable<string> Sitemap()
        {
            yield return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            yield return "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">";

            yield return "<url><loc>http://netfluid.org</loc><lastmod>"+DateTime.Now.ToString("yyyy-MM-dd")+"</lastmod><changefreq>always</changefreq><priority>0.8</priority>";
            foreach (var doc in Docs)
            {
                yield return "<url><loc>http://netfluid.org/"+Path.GetFileNameWithoutExtension(doc)+"</loc><lastmod>"+DateTime.Now.ToString("yyyy-MM-dd")+"</lastmod><changefreq>always</changefreq><priority>0.8</priority>";
            }

            yield return "</urlset>";
        }
    }
}
