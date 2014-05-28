using System;
using System.Collections.Generic;
using System.Linq;
using NetFluid.Mongo;

namespace NetFluid.Site
{
    public class BlogManager:FluidPage
    {
        public static Repository<Article> Articles;

        public static IEnumerable<Article> Latest
        {
            get { return Articles.OrderByDescending(x => x.DateTime); }
        }

        public static IEnumerable<Article> Category(string cat)
        {
            return Articles.Where(x => x.Category==cat);
        }

        static BlogManager()
        {
            Articles = new Repository<Article>("mongodb://localhost", "NetFluidSite");
        }

        [Route("/")]
        public IResponse Home()
        {
            return new FluidTemplate("embed:NetFluid.Site.UI.index.html");
        }

        [ParametrizedRoute("/read")]
        public IResponse Read(string link)
        {
            var art = Articles.FirstOrDefault(x => x.Link == link);
            return art != null ? new FluidTemplate("embed:NetFluid.Site.UI.read.html", art) : new FluidTemplate("embed:NetFluid.Site.UI.index.html");
        }

        [Route("/write")]
        public IResponse Write()
        {
            return new FluidTemplate("embed:NetFluid.Site.UI.write.html");
        }

        [Route("/add")]
        public IResponse Add()
        {
            var article = Request.Values.ToObject<Article>();
            Articles.Save(article);
            return new FluidTemplate("embed:NetFluid.Site.UI.write.html");
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
            /*foreach (var doc in Docs)
            {
                yield return "<url><loc>http://netfluid.org/"+Path.GetFileNameWithoutExtension(doc)+"</loc><lastmod>"+DateTime.Now.ToString("yyyy-MM-dd")+"</lastmod><changefreq>always</changefreq><priority>0.8</priority>";
            }
            */
            yield return "</urlset>";
        }
    }
}
