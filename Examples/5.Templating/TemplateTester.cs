using NetFluid;
using NetFluid.Razor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _5.Templating
{
    public class TemplateTester:FluidPage
    {
        [Route("/fluid")]
        public object Fluid()
        {
            return new FluidTemplate("./UI/fluid.html", "Matteo Fabbri", "matteo@phascode.org");
        }

        [Route("/razor")]
        public object Razor()
        {
            return new RazorTemplate("./UI/razor.html", new { Name = "Matteo Fabbri", Email = "matteo@phascode.org" });
        }
    }
}
