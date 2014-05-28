using NetFluid;
using System;
using System.IO;
using System.Reflection;

namespace FluidPlayer
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Path.GetFullPath(string.Join(" ", args));

            if (!File.Exists(path))
            {
                Engine.Logger.Log(LogLevel.SystemException,"Specified path "+path+" doesn't exist");
                return;
            }

            Environment.CurrentDirectory = Path.GetDirectoryName(path);

            if (!Engine.LoadAppConfiguration(path))
            {
                Engine.Logger.Log(LogLevel.Error,"App configuration not loaded, using default values");
                Engine.DefaultHost.PublicFolderManager.Add("public","/", "./Public");
                Engine.Interfaces.AddLoopBack(8080);
                Engine.Interfaces.AddAllAddresses(8080);
            }
            Engine.Load(path);
            Engine.Start();

            var cmd = "";
            do cmd = Console.ReadLine(); while (cmd != "exit");
        }
    }
}
