using Mustache;
using System.Collections.Generic;
using System.IO;

namespace NetFluid.Responses.Templates.MustacheSuperSet
{
    internal class DeCamel : InlineTagDefinition
    {
        public DeCamel() : base("decamel")
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
            var source = arguments["object"].ToString();

            for (int i = 1; i < source.Length; i++)
            {
                if (char.IsUpper(source[i]))
                {
                    source = source.Replace("" + source[i], " " + char.ToLower(source[i]));
                }
            }
            writer.Write(source.TrimStart());
        }
    }
}
