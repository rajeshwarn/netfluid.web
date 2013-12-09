using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFluid
{
    public interface IMethodExposer
    {
        Context Context { get; set; }
    }
}
