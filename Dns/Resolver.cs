using System.Linq;
using System.Net;

/*
 * Network Working Group                                     P. Mockapetris
 * Request for Comments: 1035                                           ISI
 *                                                            November 1987
 *
 *           DOMAIN NAMES - IMPLEMENTATION AND SPECIFICATION
 *
 */

namespace NetFluid.DNS
{
    /// <summary>
    ///     Resolver is the main class to do DNS query lookups
    /// </summary>
    internal class Resolver
    {
        /// <summary>
        /// DNS Client
        /// </summary>
        public Resolver()
        {
            //DnsServers = Network.Dns.Select(x => new IPEndPoint(x, 53)).ToArray();
            DnsServers =
                (new[] {new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53)}).Concat(
                    Network.Dns.Select(x => new IPEndPoint(x, 53))).ToArray();
        }


        /// <summary>
        ///     Gets or sets timeout in milliseconds
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        ///     Gets or sets list of DNS servers to use
        /// </summary>
        public IPEndPoint[] DnsServers { get; set; }
    }
}