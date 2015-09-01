using Netfluid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Threading;

namespace Example
{
    class Program
    {
        static decimal total;
        static int n=1;

        [Filter]
        public bool Filter(Context c)
        {
            c.Closed += x=> 
            {
                total += c.ElapsedTime;
                Interlocked.Increment(ref n);
            };
            return false;
        }

        [Route("/")]
        static decimal k()
        {
            return total/n;
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
