using Netfluid;
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

            var k = Json.SerializeObject(new Program());
            var d = Json.DeserializeObject(k);

            Console.WriteLine(d.GetType());

            AppDomain.CurrentDomain.UnhandledException += (s,e)=>
            {
                host.Logger.Error(e.ExceptionObject as Exception);
            };

            while (Console.ReadLine() != "end");
        }

        [Route("/")]
        public IResponse Index()
        {
            return new MustacheTemplate("./Resources/views/index.html");
        }
    }
}
