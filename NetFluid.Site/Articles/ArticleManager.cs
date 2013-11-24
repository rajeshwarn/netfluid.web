using System.Collections.Generic;
using System.IO;
using NetFluid;
using System;


namespace NetFluidService
{
    public class ArticleManager:FluidPage
    {
        [ParametrizedRoute("/delete")]
        public FluidTemplate Delete(Article article)
        {
            if (article == null)
                return new FluidTemplate("./UI/index.html");

            var user = Session<User>("user");

            if (user != null && user.Admin)
                Article.Delete(article);

            return new FluidTemplate("./Articles/UI/read.html", article);
        }

        [Route("/approve")]
        public FluidTemplate Approve(Article article)
        {
            if (article == null)
                return new FluidTemplate("./UI/index.html");

            var user = Session<User>("user");

            if (user!=null && user.Admin)
            {
                article.Approved = true;
                Article.Save(article);
            }
            return new FluidTemplate("./Articles/UI/read.html", article);
        }

        [ParametrizedRoute("/read")]
        public FluidTemplate Read(Article article)
        {
            if (article==null)
                return new FluidTemplate("./UI/index.html");

            if (Session(article.Link)==null)
            {
                Session(article.Link,DateTime.Now);
                article.Views++;
                Article.Save(article);
            }
            return new FluidTemplate("./Articles/UI/read.html",article);
        }

        [ParametrizedRoute("/write")]
        public FluidTemplate Write(Article article)
        {
            if (Session("user") == null)
                return new FluidTemplate("./Users/UI/SignIn.html");

            return new FluidTemplate("./Articles/UI/write.html",article);
        }

        [Route("/edit")]
        public FluidTemplate Edit(string title, string link, string category, string @abstract, string body)
        {
            User user = Session("user");
            if (user == null)
                return new FluidTemplate("./Users/UI/SignIn.html");

            var article = Article.Parse(link);

            if (article == null)
                return new FluidTemplate("./UI/index.html");

            if (article.Author==user.Name || user.Admin)
            {
                article.Title = title;
                article.Category = category;
                article.Abstract = @abstract;
                article.Body = body;
            }

            Article.Save(article);

            return new FluidTemplate("./Articles/UI/read.html", article);
        }

        [Route("/save")]
        public FluidTemplate Save(string title, string link,string category,string @abstract,string body)
        {
            User user = Session("user");
            if (user == null)
                return new FluidTemplate("./Users/UI/SignIn.html");

            var attachments = new List<Attachment>();
            var dir = Path.GetFullPath("./attachments");
            
            foreach (var f in Files)
            {
                var name = f.FileName;
                var p = Path.GetFullPath(Path.Combine(dir, f.Name));

                if (!p.StartsWith(dir))
                    continue;

                while (File.Exists(p))
                {
                    name = Security.UID() + f.Extension;
                    p = Path.GetFullPath(Path.Combine(dir, name));
                }

                f.SaveAs(p);
                attachments.Add(new Attachment { FileName = f.FileName, Name = name });
            }

            var article = new Article
            {
                Abstract = @abstract,
                Approved = user.Admin,
                Author = Session<User>("user").Name,
                Body = body,
                Category = category,
                DateTime = DateTime.Now,
                Link = link,
                Title = title,
                Views = 0,
                Attachment = attachments.ToArray()
            };

            Article.Save(article);

            return new FluidTemplate("./Articles/UI/read.html",article);
        }
    }
}
