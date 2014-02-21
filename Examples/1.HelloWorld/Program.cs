using System;
using System.Reflection;
using NetFluid;

namespace _1.HelloWorld
{
    /// <summary>
    /// This simple example initialize the engine and load the current assembly as default web application
    /// </summary>
    internal class Program : FluidPage
    {
        //Only method exposed o the current webapplication
        //Generate a random number and return it to the client
        [Route("/")]
        public int TryMe()
        {
            var random = new Random();
            return random.Next();
        }

        /// <summary>
        /// Self host the application
        /// </summary>
        public static void Main()
        {
            //Load adminApp.dll into the virtual host admin.netfluid.org
            Engine.LoadHost("admin.netfluid.org", Assembly.LoadFile("adminApp.dll"));

            //Virtual host www.supb.eu is FastCGI fowarded to apache.netfluid.org on port 5000
            Engine.Cluster.AddFowarding("www.supb.eu", "apache.netfluid.org:5000");

            //Load netfluid setting from App.config file, return false if missing or invalid
            if (!Engine.LoadAppConfiguration())
            {
                //Add an http interface on any available public ip on specified port
                Engine.Interfaces.AddAllAddresses(80);

                //Add an http interface on ip 127.0.0.1 on port 8080
                Engine.Interfaces.AddLoopBack(8080);
            }

            //Engine start to work
            Engine.Start();

            Console.ReadLine();
        }
    }
}