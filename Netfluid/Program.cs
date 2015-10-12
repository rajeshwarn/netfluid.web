using Netfluid;
using Netfluid.DB;
using System;

namespace Example
{
    class Program
    {
        public string name;


        static void Main(string[] args)
        { 
            var host = new NetfluidHost("*");
            host.Logger = new Netfluid.Logging.ConsoleLogger(LogLevel.Debug);
            host.PublicFolders.Add(new PublicFolder { RealPath="./Resources", VirtualPath="/cdn" });
            host.Map(typeof(Program));
            host.Start();

            var k = new KeyValueStore<Program>("ciao",x=>x.name);
            k.Get("ciiiiaaaa");
            Console.WriteLine("SUCA");

        }

        [Route("/")]
        public IResponse Index()
        {
            return new MustacheTemplate("./Resources/views/index.html");
        }
    }
}
