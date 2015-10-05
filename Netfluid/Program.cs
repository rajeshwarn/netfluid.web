using Netfluid;
using System;

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
