using System;
using System.Reflection;
using NetFluid;

namespace _1.HelloWorld
{
    internal class Program : FluidPage
    {
        [Route("/")]
        public int TryMe()
        {
            var random = new Random();
            return random.Next();
        }

        private static void Main()
        {
            Engine.Load(Assembly.GetCallingAssembly());
            Engine.Interfaces.AddInterface("127.0.0.1", 8080);
            Engine.LoadAppConfiguration();
            Engine.Start();
            Console.ReadLine();
        }
    }
}