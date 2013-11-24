using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3.UserStat
{
    public class MySite : FluidPage
    {
        [Route("/")]
        public FluidTemplate Index()
        {
            return new FluidTemplate("./Site/UI/SimplePage.html");
        }
    }
}
