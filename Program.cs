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
            var ll = "public static Route New<{0}>(Func<{0}> f) { return new Route() { MethodInfo = f.Method, Target = f.Target }; }";
            var q = new List<string>();

            for (int i = 0; i < 16; i++)
            {
                q.Add("T" + i);
                var r = ll.Replace("{0}",string.Join(",",q));

                File.AppendAllText("out.txt",r+"\r\n");
            }

            Console.ReadLine();
        }
    }
}
