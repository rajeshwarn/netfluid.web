using System;
using System.Reflection;
using NetFluid;

namespace Agenda
{
    internal class Hoster
    {
        private static void Main()
        {
            Engine.Load(Assembly.GetCallingAssembly());

            Engine.AddPublicFolder("/", "./Public", true);
            Engine.Interfaces.AddInterface("127.0.0.1", 8080);

            Engine.Start();

            Console.ReadLine();
        }
    }
}