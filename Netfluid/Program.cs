using Netfluid;
using Netfluid.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Example
{
    class Program
    {
        public string name;


        static void Main(string[] args)
        {
            var alfa = "qwertyuiopasdfghjklzxcvbnm1234567890";

            var k = new KeyValueStore<Program>("ciao");

            for (int i = 1; i < 455555; i++)
            {
                k.Insert(new string(alfa.Random(128).ToArray()), new Program());
                if (i % 2000 == 0) Console.WriteLine("LOADING " + i);
            }

            Console.WriteLine("SUCA");

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
