using Netfluid;
using System;
using System.IO;
using System.Linq.Expressions;

namespace Example
{
    class Program
    {
        [Filter]
        public bool Filter()
        {
            Console.Write("FILTRATO !");
            return false;
        }

        static int k()
        {
            return 888;
        }

        static void Main(string[] args)
        {
            Engine.DefaultHost.Map(typeof(Program));

            Engine.Start();
        }
    }
}
