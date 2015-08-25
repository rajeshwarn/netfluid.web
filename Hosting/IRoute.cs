using System.Reflection;

namespace Netfluid
{
    public interface IRoute
    {
        string[] GroupNames { get; }
        string HttpMethod { get; set; }
        int Index { get; set; }
        MethodInfo MethodInfo { get; set; }
        string Name { get; set; }
        ParameterInfo[] Parameters { get; }
        object Target { get; set; }
        string Url { get; set; }

        bool Handle(Context cnt);
    }
}