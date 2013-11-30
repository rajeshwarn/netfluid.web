using System;
using System.Reflection;
using NetFluid;

namespace _7.Authentication
{
    class Hoster
    {
        static void Main()
        {
            // Enable console log of recieved request
            Engine.DevMode = true;

            // Set all files inside "Public" as public and downloadable by the client (styles, js, images..)
            Engine.AddPublicFolder("/","./Public",true);

            // Add an HTTP interfaces on our application server
            Engine.Interfaces.AddAllAddresses(8080);
            Engine.Interfaces.AddInterface("127.0.0.1", 8080);

            // Makes the web application runs
            Engine.Start();

            //Prevent application from closing !
            Console.ReadLine();
        }
    }
}
