using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Service
{
    class DefaultExposer:MethodExposer
    {
        [CallOn(StatusCode.AnyError)]
        public IResponse NotFound()
        {
            return new NetFluid.Templates.MustacheResponse("./ui/404.html");
        }
    }
}
