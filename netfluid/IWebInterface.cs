using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace NetFluid
{
    /// <summary>
    /// HTTP/S interface for the Engine
    /// </summary>
    public interface IWebInterface
    {
        /// <summary>
        /// PFX certificate for HTTPS connection
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// IP and port on wich listen
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Listener of the interface
        /// </summary>
        Socket Socket { get; }

        /// <summary>
        /// Start accepting clients
        /// </summary>
        void Start();
    }
}