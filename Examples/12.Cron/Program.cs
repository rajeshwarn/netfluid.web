using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _12.Cron
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                d = NetFluid.Cron.Cron.Next("* * 13 JUL SAT *", d);
                Console.WriteLine("["+ i +"]" + d);
            }
            Console.ReadLine();
        }
    }
}
