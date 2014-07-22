using System;
using System.IO;
using System.Net;

namespace NetFluid.Site
{
    class AutoHoster
    {
        public static void Main()
        {
            Engine.Host("cdn.netfluid.org").PublicFolderManager.Add("cdn", "/", @"C:\Users\netfluid\Desktop\Release\CDN\netfluid.org");
            Engine.Interfaces.AddLoopBack();
            Engine.Interfaces.AddAllAddresses();
            Engine.Start();
            Console.ReadLine();
        }
    }
}
