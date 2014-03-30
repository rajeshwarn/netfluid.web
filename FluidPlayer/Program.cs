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

            Environment.CurrentDirectory = Path.GetDirectoryName(path);

            if (!Engine.LoadAppConfiguration(path+".config"))
            {
                Engine.Logger.Log(LogLevel.Error,"App configuration not loaded, using default values");
                Engine.AddPublicFolder("/", "./Public", false);
                Engine.Interfaces.AddLoopBack(8080);
                Engine.Interfaces.AddAllAddresses(8080);
            }
            Engine.Load(Assembly.LoadFile(path));
            Engine.Start();

            var cmd = "";
            do cmd = Console.ReadLine(); while (cmd != "exit");
        }
    }
}
