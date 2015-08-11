using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFluid;
using System.Net.Mail;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine.Prefixes.Add("http://*/");
            Engine.Start();
        }
    }
}
