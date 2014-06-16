using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Site
{
    class AutoHoster
    {
        public static void Main()
        {
            Console.WriteLine((DateTime.Now - (new DateTime(2014,3,27))).Days);
            if (!Engine.LoadAppConfiguration())
            {
                Engine.Logger.Log(LogLevel.Error, "App configuration not loaded, using default values");
                Engine.DefaultHost.PublicFolderManager.Add("public", "/", "./Public");
                Engine.Interfaces.AddLoopBack(80);
                Engine.Interfaces.AddAllAddresses(80);
            }
            Engine.DevMode = false;
            Engine.Start();
            Console.ReadLine();
        }
    }
}
