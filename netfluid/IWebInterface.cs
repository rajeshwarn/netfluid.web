using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace NetFluid
{
    public interface IWebInterface
    {
        X509Certificate2 Certificate { get; }
        IPEndPoint Endpoint { get; }
        Socket Socket { get; }
        void Start();
    }
}