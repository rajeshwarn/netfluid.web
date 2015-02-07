using Mustache;
using NetFluid.Responses.Templates.MustacheSuperSet;
using System.Collections.Generic;
using System.IO;


using System;

namespace NetFluid
{
    public class MustacheTemplate : IResponse
    {
        FormatCompiler compiler;
        Generator generator;
        object args;
        string templateFile;

        static List<TagDefinition> customTags;
        static AutoCache<string> cache;


        static MustacheTemplate()
        {
            customTags = new List<TagDefinition>();
            customTags.Add(new DeCamel());
            customTags.Add(new First());
            customTags.Add(new Include());
            customTags.Add(new Value());

            cache = new AutoCache<string>
            {
                AutoRenew = true,
                Expiration = TimeSpan.FromHours(1),
                Get = File.ReadAllText,
            };
        }

        public static void AddCustomTag(TagDefinition tag)
        {
            customTags.Add(tag);
        }

        public MustacheTemplate(string templateFile): this(templateFile,null)
        { 
        }

        public MustacheTemplate(string templateFile, object args)
        {
            this.templateFile = templateFile;

            if(File.Exists(templateFile))
            {
                compiler = new FormatCompiler();
                compiler.RemoveNewLines = false;
                this.args = args;

                customTags.ForEach(x => compiler.RegisterTag(x, true));

                generator = compiler.Compile(cache[Path.GetFullPath(templateFile)]);
            }
        }

        public void SetHeaders(Context cnt)
        {
            
        }

        public void SendResponse(Context cnt)
        {
            if(generator == null)
            {
                cnt.Writer.Write(templateFile + " does not exists");
                return;
            }

            generator.Render(args,cnt.Writer);
        }

        public static string Parse(string mustache, object args)
        {
            var compiler = new FormatCompiler();
            compiler.RemoveNewLines = false;
            customTags.ForEach(x => compiler.RegisterTag(x, true));

            var writer = new StringWriter();
            var generator = compiler.Compile(mustache);
            generator.Render(args, writer);

            return writer.ToString();
        }

        public void Dispose()
        {
            compiler = null;
            generator = null;
        }
    }
}
 