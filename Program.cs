using Netfluid;
using System;
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
            host.Start();
            
            host.Routes.Add()

            Console.ReadLine();
        }
    }
}
