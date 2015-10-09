using Netfluid;
using Netfluid.Json;
using Netfluid.Json.Linq;
using System;

namespace Example
{
    class Program
    {
        public int KKK = 777;

        public Ivan Ivanovick = new Ivan();

        public class Ivan
        {
            public string Name { get; set; } = "Il coglionazzo";
        }

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
