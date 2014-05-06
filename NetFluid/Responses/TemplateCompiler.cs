// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace NetFluid.Responses
{
    internal class TemplateCompiler
    {
        private static readonly ConcurrentDictionary<string, MethodInfo> templates;

        static TemplateCompiler()
        {
            templates = new ConcurrentDictionary<string, MethodInfo>();
        }

        internal static MethodInfo Get(string filename,Type fromType)
        {
            var path = filename.StartsWith("embed:") ? filename : Path.GetFullPath(filename);

            MethodInfo template;
            if (templates.TryGetValue(path, out template))
                return template;

            try
            {
                template = Load(path,fromType);
                templates.AddOrUpdate(path, template, (x, y) => y);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Error, "Error loading template " + filename, ex);
                template = Empty();
                templates.AddOrUpdate(path, template, (x, y) => y);
            }

            return template;
        }

        private static string Escape(string from)
        {
            return from.Replace("\a", "\\a")
                .Replace("\\", "\\\\")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\v", "\\v")
                .Replace("\'", "\\\'")
                .Replace("\"", "\\\"");
        }

        private static string CleanCode(string code)
        {
            if (code.StartsWith("page "))
                return code;


            code = Regex.Replace(code, " *\\( *", "(");
            code = Regex.Replace(code, " *\\) *", ")");
            code = Regex.Replace(code, " *\\{ *", "{");
            code = Regex.Replace(code, " *\\} *", "}");

            //code = Regex.Replace(code, Regex.Escape("#") + ".+", "");
            //code = Regex.Replace(code, Regex.Escape("//") + ".+", "");

            var operators = new[]
                                {
                                    "+", "-", "!", "~", "++", "--", "&", "*", "/", "%", "+", "-", "<=", ">=", "==", "!="
                                    ,
                                    "&", "^", "|", "&&", "||", "?:", "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^="
                                    ,
                                    "<<=", ">>=", "??", ","
                                };

            code = operators.Aggregate(code,
                                       (current, op) => Regex.Replace(current, "\\s*" + Regex.Escape(op) + "\\s*", op));

            code = Regex.Replace(code, "\\s+" + Regex.Escape("[]") + "\\s*", "[]");

            foreach (Match m in Regex.Matches(code, "\\D\\s{2,}\\D"))
            {
                var chars = m.Value.ToCharArray();
                var first = chars.First();
                var last = chars.Last();

                code = code.Replace(m.Value, first + " " + last);
            }

            //Remove multiline comment
            code = Regex.Replace(code, "/\\*.*?\\*/", "");

            code = Regex.Replace(code, "\\s*" + Regex.Escape("{") + "\\s*", "{");
            code = Regex.Replace(code, "\\s*" + Regex.Escape("}") + "\\s*", "}");

            code = Regex.Replace(code, "\\s*" + Regex.Escape(";") + "\\s*", ";");

            return code;
        }

        private static MethodInfo Load(string filename,Type fromType)
        {
            Stream stream;

            if (filename.StartsWith("embed:"))
            {
                stream = fromType.Assembly.GetManifestResourceStream(filename.Substring("embed:".Length));

                if (stream == null)
                    return FileNotFound(filename);
            }
            else
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    }
                    catch (FileNotFoundException)
                    {
                        return FileNotFound(filename);
                    }
                }
                else
                {
                    return FileNotFound(filename);
                }
            }

            var reader = new StreamReader(stream);
            var html = reader.ReadToEnd();

            var lines = html.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder();
            var flow = new[] {"foreach(", "for(", "if(", "while(", "do", "switch(", "else"};
            var parameters = "(NetFluid.Context Context)";
            var parametersLine = 1;

            var usings = new List<string>(new[]
                                              {
                                                  "using System;",
                                                  "using NetFluid;",
                                              });

            for (var i = 0; i < lines.Length; i++)
            {
                var r = lines[i].Trim();

                if (r == "")
                    continue;

                if (r[0] == '%')
                {
                    var row = CleanCode(r.Substring(1).Trim());

                    #region SPECIAL INSTRUCTIONS

                    if (row.StartsWith("end "))
                    {
                        builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                        builder.AppendLine("}");
                    }
                    else if (row.StartsWith("else if"))
                    {
                        builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                        builder.AppendLine("}\r\n" + row + "\r\n{\r\n");
                    }
                    else if (row == "else")
                    {
                        builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                        builder.AppendLine("}\r\nelse\r\n{\r\n");
                    }
                    else if (flow.Any(row.StartsWith))
                    {
                        builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                        if (row.StartsWith("while(") && row.EndsWith(';'))
                            builder.AppendLine("\r\n}\r\n" + row);
                        else
                            builder.AppendLine(row + "\r\n{\r\n");
                    }
                    else if (row.StartsWith("using "))
                    {
                        usings.Add(row);
                    }
                    else if (row.StartsWith("template:parameters"))
                    {
                        parameters = row.Substring("template:parameters".Length);

                        if (parameters == "()")
                            parameters = "(NetFluid.Context Context)";
                        else
                            parameters = "(NetFluid.Context Context," + parameters.Substring(1);

                        parametersLine = i + 1;
                    }
                    else
                    {
                        builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                        builder.AppendLine(row);
                    }

                    #endregion
                }
                else
                {
                    if (r.Contains("{%"))
                    {
                        #region variables

                        while (r.Contains("{%") && r.Contains("%}"))
                        {
                            var index = r.IndexOf("{%", System.StringComparison.Ordinal);
                            var index2 = r.IndexOf("%}", System.StringComparison.Ordinal);

                            var variabile = r.Substring(index + 2, index2 - index - 2);
                            var pre = r.Substring(0, index);
                            var post = r.Substring(index2 + 2);

                            builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                            builder.AppendLine("Context.Writer.Write(\"" + Escape(pre) + "\");");
                            builder.AppendLine("Context.Writer.Write((" + variabile + ")==null?\"null\":(" + variabile +
                                               ").ToString());");

                            if (post.Contains("{%"))
                            {
                                r = post;
                            }
                            else
                            {
                                builder.AppendLine("Context.Writer.Write(\"" + Escape(post) + "\");");
                                r = "";
                            }
                        }
                        builder.AppendLine("#line " + (i + 1) + " \"" + filename + "\"");
                        builder.AppendLine("Context.Writer.Write(\"" + Escape(r) + "\");");

                        #endregion
                    }
                    else
                    {
                        builder.AppendLine("Context.Writer.WriteLine(\"" + Escape(r) + "\");");
                    }
                }
            }

            var @namespace = fromType.Namespace;
            var name = "____FluidTemplate" + Security.UID();

            var classBuilder = new StringBuilder();
            classBuilder.AppendLine(string.Join("\r\n", usings.Distinct()));
            classBuilder.AppendLine("namespace " + @namespace + "\r\n{\r\n");
            classBuilder.AppendLine("public static class " + name + "\r\n{\r\n");

            #region INCLUDE FUNCTION
            classBuilder.AppendLine("public static void Include(Context c, string path, params object[] args)\r\n{\r\n");
            classBuilder.AppendLine("var k = new FluidTemplate(path,args);\r\n");
            classBuilder.AppendLine("k.SendResponse(c);\r\n");
            classBuilder.AppendLine("\r\n}\r\n");
            #endregion

            classBuilder.AppendLine("#line " + parametersLine + " \"" + filename + "\"");
            classBuilder.AppendLine("public static void Run" + parameters + "\r\n{\r\n");
            classBuilder.AppendLine(builder.ToString());
            classBuilder.AppendLine("\r\n}\r\n");
            classBuilder.AppendLine("\r\n}\r\n");
            classBuilder.AppendLine("\r\n}\r\n");


            var csc = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var csc_parameters = new CompilerParameters(new string[] {}, Path.GetTempFileName(), false);
            csc_parameters.TreatWarningsAsErrors = false;

            var refe = fromType.Assembly.GetReferencedAssemblies();
            foreach (var reference in refe)
            {
                var ass = Assembly.Load(reference);
                csc_parameters.ReferencedAssemblies.Add(ass.Location);
            }

            csc_parameters.ReferencedAssemblies.Add(fromType.Assembly.Location);

            csc_parameters.GenerateInMemory = true;
            csc_parameters.GenerateExecutable = false;
            csc_parameters.TreatWarningsAsErrors = false;
            csc_parameters.CompilerOptions = "/optimize /nowarn:108;114;3009;1685";
            csc_parameters.WarningLevel = 1;

            var code = classBuilder.ToString();

            var results = csc.CompileAssemblyFromSource(csc_parameters, code);

            if (results.Errors.HasErrors)
            {
                if (Engine.DevMode)
                    return CompilationError(results.Errors, filename);

                foreach (CompilerError err in results.Errors)
                {
                    Engine.Logger.Log(LogLevel.Error,"Compilation error "+err.ErrorNumber+" "+err.ErrorText+" on line "+err.Line+" of file "+err.FileName);
                }
                FluidTemplate.OnError(results.Errors);
            }

            return results.CompiledAssembly.GetTypes()[0].GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        }

        private static MethodInfo CompilationError(CompilerErrorCollection errors, string filename)
        {
            Engine.Logger.Log(LogLevel.Error, "Runtime compilation of " + filename + " failed");

            if (!Engine.DevMode)
                return Empty();

            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using NetFluid;");
            builder.AppendLine("namespace NetFluid");
            builder.AppendLine("{");
            builder.AppendLine("public static class  ______NetFluidCompilationError" + Security.UID());
            builder.AppendLine("{");
            builder.AppendLine("public static void Run(NetFluid.Context Context)");
            builder.AppendLine("{");
            builder.AppendLine("Context.Response.StatusCode = StatusCode.InternalServerError;");
            builder.AppendLine("Context.SendHeaders();");
            builder.AppendLine("Context.Writer.WriteLine(\"<html>\");");
            builder.AppendLine("Context.Writer.WriteLine(\"<h1>Compilation errors</h1>\");");
            builder.AppendLine(
                "Context.Writer.WriteLine(\"<div>To turn off this view set the DevMode value on false in AppConfig on by code with NetFluid.Engine.DevMode</div>\");");
            foreach (CompilerError err in errors)
            {
                builder.AppendLine("Context.Writer.WriteLine(\"<h2>" + err.ErrorNumber + " - " + Escape(err.ErrorText) +
                                   "</h2>\");");
                builder.AppendLine("Context.Writer.WriteLine(\"<div>\");");
                builder.AppendLine("Context.Writer.WriteLine(\"<span>Line: " + err.Line + "  file:" + Escape(filename) +
                                   "</span> \");");
                builder.AppendLine("Context.Writer.WriteLine(\"</div>\");");
                builder.AppendLine("Context.Writer.WriteLine(\"<div><a href=\\\"http://netfluid.org/help/" +
                                   err.ErrorNumber + "\\\">http://netfluid.org/help/" + err.ErrorNumber +
                                   "</a></div>\");");
            }
            builder.AppendLine("Context.Writer.WriteLine(\"</html>\");");
            builder.AppendLine("Context.Close();");

            builder.AppendLine("}");
            builder.AppendLine("}");
            builder.AppendLine("}");

            var csc = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var csc_parameters = new CompilerParameters(new string[] {}, Path.GetTempFileName(), false);
            csc_parameters.TreatWarningsAsErrors = false;

            foreach (var reference in Assembly.GetEntryAssembly().GetReferencedAssemblies())
            {
                var ass = Assembly.Load(reference);
                csc_parameters.ReferencedAssemblies.Add(ass.Location);
            }
            csc_parameters.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);

            csc_parameters.GenerateInMemory = true;
            csc_parameters.GenerateExecutable = false;
            csc_parameters.TreatWarningsAsErrors = false;
            csc_parameters.CompilerOptions = "/optimize /nowarn:108;114;3009;1685";
            csc_parameters.WarningLevel = 1;
            var results = csc.CompileAssemblyFromSource(csc_parameters, builder.ToString());

            return results.CompiledAssembly.GetTypes()[0].GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        }

        private static MethodInfo FileNotFound(string filename)
        {
            Engine.Logger.Log(LogLevel.Error, "Template " + filename + " not found");

            if (!Engine.DevMode)
                return Empty();

            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using NetFluid;");
            builder.AppendLine("namespace NetFluid");
            builder.AppendLine("{");
            builder.AppendLine("public static class  ______NetFluidCompilationError" + Security.UID());
            builder.AppendLine("{");
            builder.AppendLine("public static void Run(NetFluid.Context Context)");
            builder.AppendLine("{");
            builder.AppendLine("Context.Response.StatusCode = StatusCode.InternalServerError;");
            builder.AppendLine("Context.SendHeaders();");
            builder.AppendLine("Context.Writer.WriteLine(\"<html>\");");
            builder.AppendLine("Context.Writer.WriteLine(\"<h1>Compilation error - File not found:" + Escape(filename) +
                               "</h1>\");");
            builder.AppendLine(
                "Context.Writer.WriteLine(\"<div>To turn off this view set the DevMode value on false in AppConfig on by code with NetFluid.Engine.DevMode</div>\");");
            builder.AppendLine("Context.Writer.WriteLine(\"</html>\");");
            builder.AppendLine("Context.Close();");

            builder.AppendLine("}");
            builder.AppendLine("}");
            builder.AppendLine("}");

            var csc = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var csc_parameters = new CompilerParameters(new string[] {}, Path.GetTempFileName(), false);
            csc_parameters.TreatWarningsAsErrors = false;

            foreach (var reference in Assembly.GetEntryAssembly().GetReferencedAssemblies())
            {
                var ass = Assembly.Load(reference);
                csc_parameters.ReferencedAssemblies.Add(ass.Location);
            }
            csc_parameters.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);

            csc_parameters.GenerateInMemory = true;
            csc_parameters.GenerateExecutable = false;
            csc_parameters.TreatWarningsAsErrors = false;
            csc_parameters.CompilerOptions = "/optimize /nowarn:108;114;3009;1685";
            csc_parameters.WarningLevel = 1;
            var results = csc.CompileAssemblyFromSource(csc_parameters, builder.ToString());

            return results.CompiledAssembly.GetTypes()[0].GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        }

        private static MethodInfo Empty()
        {
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using NetFluid;");
            builder.AppendLine("namespace NetFluid");
            builder.AppendLine("{");
            builder.AppendLine("public static class  ______NetFluidCompilationError" + Security.UID());
            builder.AppendLine("{");
            builder.AppendLine("public static void Run(NetFluid.Context Context)");
            builder.AppendLine("{");
            builder.AppendLine("Context.Response.StatusCode = StatusCode.BadRequest;");
            builder.AppendLine("Context.SendHeaders();");
            builder.AppendLine("Context.Writer.WriteLine(\"<!doctype html>\");");
            builder.AppendLine("Context.Writer.WriteLine(\"<html>\");");
            builder.AppendLine("Context.Writer.WriteLine(\"</html>\");");
            builder.AppendLine("Context.Close();");

            builder.AppendLine("}");
            builder.AppendLine("}");
            builder.AppendLine("}");

            var csc = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var csc_parameters = new CompilerParameters(new string[] {}, Path.GetTempFileName(), false);
            csc_parameters.TreatWarningsAsErrors = false;

            foreach (var reference in Assembly.GetEntryAssembly().GetReferencedAssemblies())
            {
                var ass = Assembly.Load(reference);
                csc_parameters.ReferencedAssemblies.Add(ass.Location);
            }
            csc_parameters.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);

            csc_parameters.GenerateInMemory = true;
            csc_parameters.GenerateExecutable = false;
            csc_parameters.TreatWarningsAsErrors = false;
            csc_parameters.CompilerOptions = "/optimize /nowarn:108;114;3009;1685";
            csc_parameters.WarningLevel = 1;
            var results = csc.CompileAssemblyFromSource(csc_parameters, builder.ToString());

            return results.CompiledAssembly.GetTypes()[0].GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        }
    }
}