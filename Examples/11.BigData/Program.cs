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
            var cache = new NetFluid.Collections.InMemoryOlap();
            var rand = new Random();

            for (int k = 0; k < 28; k++)
            {
                for (int l = 0; l < 28; l++)
                {
                    for (int m = 0; m < 28; m++)
                    {
                        for (int n = 0; n < 28; n++)
                        {
                            for (int o = 0; o < 28; o++)
                            {
                                var value = k*l*m*n*o;
                                cache.Set(value, k, l, m, n, o);
                            }
                        }
                    }
                }
            }

            Console.WriteLine(cache.Count);

            for (int j = 0; j < 5; j++)
                for (int k = 0; k < 5; k++)
                {
                    for (int l = 0; l < 5; l++)
                    {
                        for (int m = 0; m < 5; m++)
                        {
                            for (int n = 0; n < 5; n++)
                            {
                                for (int o = 0; o < 5; o++)
                                {
                                    var value = j * k * l * m * n * o;
                                    if(value!=(int)cache.Get(value, j, k, l, m, n, o))
                                        Console.WriteLine("ERROR");
                                }
                            }
                        }
                    }
                }

            Console.WriteLine("End ! :D");
            Console.ReadLine();
        }
    }
}
