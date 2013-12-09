using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _9.RegexRoutes
{
    class Program : FluidPage
    {
        //Ex: http://localhost:8080/Mattew
        [RegexRoute("/(?<name>[a-zA-Z]+)")]
        public string Person(string name)
        {
            return "This is a person, his name is " + name; 
        }

        //Ex: http://localhost:8080/232131
        [RegexRoute("/(?<number>[\\d]+)")]
        public string Person(int number)
        {
            return "This is a number, his value is " + number;
        }

        static void Main(string[] args)
        {
            Engine.Interfaces.AddLoopBack(8080);
            Engine.Start();
            Console.ReadLine();
        }
    }
}
