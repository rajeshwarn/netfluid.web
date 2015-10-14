using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Netfluid
{
    public class Trigger:Route
    {
        public Trigger(dynamic funcOrAction)
        {
            method = funcOrAction;
        }

        public Trigger(MethodInfo mi, object instance):base(mi,instance)
        {
        }

        internal override dynamic Handle(Context cnt)
        {
            Task.Factory.StartNew(() => base.Handle(cnt));
            return true;
        }
    }
}
