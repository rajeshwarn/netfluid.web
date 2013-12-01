using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _5.Kendo
{
    class Program
    {
        static void Main(string[] args)
        {
            #region APPLICATION SELF HOST
            // Setup the application server, hosting the current assembly
            Engine.Load(Assembly.GetCallingAssembly());

            // Enable console log of recieved request
            Engine.DevMode = true;

            // Set all files inside "Public" as public and downloadable by the client (styles, js, images..)
            Engine.AddPublicFolder("/", "./Public", true);

            // Add an HTTP interface on our application server
            Engine.Interfaces.AddInterface("127.0.0.1", 8080);

            // Makes the web application runs
            Engine.Start();

            //Prevent application from closing !
            Console.ReadLine();
            #endregion
        }
    }
}
