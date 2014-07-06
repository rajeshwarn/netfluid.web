using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Site
{
    public class Download :  FluidPage
    {
        [Route("/download")]
        public IResponse Page()
        {
            return new FluidTemplate("embed:NetFluid.Site.UI.download.html");
        }
    }
}
