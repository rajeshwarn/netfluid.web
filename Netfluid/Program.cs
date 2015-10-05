using Netfluid;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.Mail;

namespace Example
{
    class Program
    {
        [StatusCodeHandler(StatusCode.NotFound)]
        public IResponse AL(Context cnt)
        {
            return new StringResponse("CIAO");
        }

        static void Main(string[] args)
        { 
            var host = new NetfluidHost("*");
            host.Logger = new Netfluid.Logging.ConsoleLogger(LogLevel.Debug);
            host.Map(typeof(Program));
            host.Start();

            AppDomain.CurrentDomain.UnhandledException += (s,e)=>
            {
                host.Logger.Error(e.ExceptionObject as Exception);
            };


            while (Console.ReadLine() != "end");
        }
    }
}
