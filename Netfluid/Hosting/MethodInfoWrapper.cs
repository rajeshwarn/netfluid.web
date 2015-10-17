using System.Reflection;

namespace Netfluid
{
    class MethodInfoWrapper
    {
        internal object Target;

        internal MethodInfo MethodInfo;

        public object Invoke(object[] parameters)
        {
            return MethodInfo.Invoke(Target, parameters);
        }
    }
}
