using Netfluid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;

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
            var host = new Host("*");
            host.Map(typeof(Program));
            host.Start();

            Console.ReadLine();
        }
    }
}
