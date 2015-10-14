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

            var k = new KeyValueStore<Program>("ciao", x => x.name);

            for (int l = 0; l < 2000; l++)
            {
                var list = new List<Program>();

                for (int i = 1; i < 50000; i++)
                {
                    list.Add(new Program { name = new string(alfa.Random(80).ToArray()) });
                    if (i % 2000 == 0) Console.WriteLine("LOADING " + i);
                }

                for (int i = 1; i < list.Count; i++)
                {
                    k.Insert(list[i]);
                    if (i % 2000 == 0) Console.WriteLine("LOADING " + i);
                }
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
