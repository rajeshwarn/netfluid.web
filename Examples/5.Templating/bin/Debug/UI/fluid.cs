using System;
using NetFluid;
namespace _5.Kendo
{

public static class ____FluidTemplatef1ea601b6420426fb8323660254ed622
{

public static void Include(Context c, string path, params object[] args)
{

var k = new FluidTemplate(path,args);

k.SendResponse(c);


}

#line 1 "C:\Users\Matteo\Desktop\NetFluid\netfluid\Examples\5.Templating\bin\Debug\UI\fluid.html"
public static void Run(NetFluid.Context Context,string name,string mail);
{

Context.Writer.WriteLine("<html>");
Context.Writer.WriteLine("<head>");
Context.Writer.WriteLine("<title>Hello @Model.Name</title>");
Context.Writer.WriteLine("</head>");
Context.Writer.WriteLine("<body>");
Context.Writer.WriteLine("Email: @Html.TextBoxFor(m => m.Email)");
Context.Writer.WriteLine("</body>");
Context.Writer.WriteLine("</html>");


}


}


}

