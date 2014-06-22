using System;

namespace NetFluid.Site
{
    class AutoHoster
    {
        public static void Main()
        {
            if (!Engine.LoadAppConfiguration())
            {
                Engine.Logger.Log(LogLevel.Error, "App configuration not loaded, using default values");
                Engine.DefaultHost.PublicFolderManager.Add("public", "/", "./Public");
                Engine.Interfaces.AddLoopBack();
                Engine.Interfaces.AddAllAddresses();
            }
            Engine.Start();
            Console.ReadLine();
        }
    }
}
