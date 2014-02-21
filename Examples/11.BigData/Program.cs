using NetFluid.Collections.Persistent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _11.BigData
{
    class Program
    {
        static void Main(string[] args)
        {
            var cache = new NetFluid.Collections.NDimensionalDictionary<int>();
            var rand = new Random();
            var coords = new int[32];

            for (int i = 0; i < 4096*4096; i++)
            {
                for (int j = 0; j < coords.Length; j++)
                {
                    coords[j] = rand.Next();
                    rand = new Random(rand.Next());
                }
                cache.Set(rand.Next(), coords.Cast<object>().ToArray());

                if (i%1000==0)
                {
                    Console.WriteLine(i);
                }
            }
            Console.WriteLine("End ! :D");
            Console.ReadLine();
        }
    }
}
