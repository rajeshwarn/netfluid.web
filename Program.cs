using Netfluid;
using System;
using System.IO;

namespace Example
{
    class Program
    {
        [Route("/")]
        static IResponse k()
        {
            return new StringResponse("suca");
        }

        [Filter]
        public dynamic Filter(bool t)
        {
            Console.Write("FILTRATO !");
            return true;
        }

        static void Main(string[] args)
        {
            Engine.DefaultHost.Map(typeof(Program));
            Engine.Start();
        }
    }
}
