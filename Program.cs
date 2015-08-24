using Netfluid;
using System;

namespace Example
{
    class Program
    {
        [Route("/")]
        static int k(int d)
        {
            return 0;
        }

        static void Main(string[] args)
        {
            Engine.DefaultHost.Load(typeof(Program));

            Engine.Start();
        }
    }
}
