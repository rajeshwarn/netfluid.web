using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFluid;

namespace RaspSite
{
    class Program : FluidPage
    {
        [Route("/")]
        public string Index()
        {
            return Performance.Avg.ToString();
        }

        static void Main(string[] args)
        {
            if (!Engine.LoadAppConfiguration())
            {
                Engine.Logger.Log(LogLevel.Error, "App configuration not loaded, using default values");
                Engine.DefaultHost.PublicFolderManager.Add("public", "/", "./Public");
                Engine.Interfaces.AddLoopBack(80);
                Engine.Interfaces.AddAllAddresses(80);
            }
            Engine.DevMode = false;

            Performance.LoadPerformance();

            Engine.Start();
            Console.ReadLine();
        }
    }
}
