using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8.Configuration
{
    class Program
    {
        //NETFLUID SETTINGS ARE DEFINED INTO THE APPCONFIG
        static void Main(string[] args)
        {
            Engine.LoadAppConfiguration();

            //Will be called on ANY request
            Engine.SetController(x =>
            {
                x.Writer.WriteLine("HI THERE !");
            });

            Engine.Start();
            Console.ReadLine();
        }
    }
}
