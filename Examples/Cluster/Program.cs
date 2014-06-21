using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFluid;

namespace Cluster
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine.DefaultHost.SetController(x =>
            {
                x.FowardTo("http://www.facebook.com/");
            });
            Engine.DevMode = true;
            Engine.Interfaces.AddLoopBack();
            Engine.Interfaces.AddAllAddresses(80);
            Engine.Start();
            Console.ReadLine();
        }
    }
}
