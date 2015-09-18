using Mustache;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Netfluid.Responses.Templates.MustacheSuperSet
{
    internal class HtmlList : InlineTagDefinition
    {
        public HtmlList(): base("li")
        {
        }

        protected override IEnumerable<TagParameter> GetParameters()
        {
            return new TagParameter[]
            {
                new TagParameter("object")
            };
        }

        public override void GetText(TextWriter writer, Dictionary<string, object> arguments, Scope context)
        {
            var source = arguments["object"] as IEnumerable;
            int count = 0;

            writer.Write("<ul>");
            foreach (var item in source)
            {
                writer.Write("<li>"+count+"</li>");
            }
            writer.Write("</ul>");
        }
    }
}
