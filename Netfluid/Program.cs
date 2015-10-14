using Netfluid;
using Netfluid.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new NetfluidHost("*");
            host.Logger = new Netfluid.Logging.ConsoleLogger(LogLevel.Debug);
            host.PublicFolders.Add(new PublicFolder { RealPath="./Resources", VirtualPath="/cdn" });
            host.Map(typeof(Program));
            host.Start();

        }

        [Route("/")]
        public IResponse Index()
        {
            return new MustacheTemplate("./Resources/views/index.html");
        }
    }
}
