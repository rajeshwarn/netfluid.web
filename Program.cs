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
            HttpListener l1 = new HttpListener();
            l1.Prefixes.Add("http://localhost/");

            HttpListener l2 = new HttpListener();
            l2.Prefixes.Add("http://gimbo.gim/");

            l1.Start();
            l2.Start();

            HttpListener l3 = new HttpListener();
            l3.Prefixes.Add("http://*/");

            l3.Start();

            HttpListener l4 = new HttpListener();
            l4.Prefixes.Add("https://*/");
            l4.Start();


            Console.ReadLine();
        }
    }
}
