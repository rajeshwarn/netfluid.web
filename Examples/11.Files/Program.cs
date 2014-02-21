using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _11.Files
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine.Interfaces.AddLoopBack(8080);
            Engine.Start();
            Console.ReadLine();
        }
    }
}
