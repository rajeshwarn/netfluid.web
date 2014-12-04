using Mustache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetFluid.Templates
{
    public class MustacheResponse : IResponse
    {
        FormatCompiler compiler;
        Generator generator;
        object args;

        public MustacheResponse(string templateFile): this(templateFile,null)
        { 
        }

        public MustacheResponse(string templateFile, object args)
        {
            compiler = new FormatCompiler();
            compiler.RemoveNewLines = false;
            this.args = args;

            generator = compiler.Compile(File.ReadAllText(templateFile));
        }

        public void SetHeaders(Context cnt)
        {
            
        }

        public void SendResponse(Context cnt)
        {
            string result = generator.Render(args);
            cnt.Writer.Write(result);
        }
    }
}
 