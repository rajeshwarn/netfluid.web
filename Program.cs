using Netfluid;
using System;
using System.IO;

namespace Example
{
    class Program
    {
        [Route("/")]
        static IResponse k()
        {
            return new StringResponse("suca");
        }

        static void Main(string[] args)
        {
            Delegate f = (Func<IResponse>)k;

            Engine.DefaultHost.Load(typeof(Program));

            Engine.Start();
        }
    }
}
