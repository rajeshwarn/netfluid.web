using System;
using System.Reflection;

namespace Netfluid
{
    public class Filter:Route
    {
        public Filter(dynamic funcOrAction)
        {
            method = funcOrAction;
        }

        public Filter(MethodInfo mi, object instance):base(mi,instance)
        {
        }
    }
}
