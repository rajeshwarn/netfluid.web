using System;
using System.Linq;

namespace NetFluid.Site
{
    public class Article:IDatabaseObject
    {
        public string Id { get; set; }

        private string title;

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                Link = value.ToLowerInvariant();
                title.Where(x => (x < 'a' || x > 'z') && (x < '1' || x >'9') && x!='0' ).ForEach(c=> Link = Link.Replace(c,'_'));
            }
        }

        public string Category;

        public string Abstract;
        public string Body;
        public DateTime DateTime;

        public string Link;

        public Article()
        {
            DateTime = DateTime.Now;
        }
    }
}
