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

        public dynamic Filter(bool t)
        {
            if(t)return true;
        }

        static void Main(string[] args)
        {

            Engine.Start();
        }
    }
}
