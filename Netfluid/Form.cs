using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid
{
    class Form
    {
        public string Generate(Type type, string action)
        {
            var sb = new StringBuilder();
            sb.Append("<form method=\"post\" action=\""+action+"\">");

            foreach (var item in type.GetFields())
            {
                sb.Append("<div class=\"form-group\">");
                sb.Append("<label>Email address</label>");
                sb.Append("</div>");
            }

            sb.Append("</form>");

            return sb.ToString();
        }
    }
}
