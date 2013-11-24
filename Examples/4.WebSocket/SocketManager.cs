using NetFluid;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4.WebSocket
{
    public class SocketManager:FluidPage
    {
        //Store clients to foward incoming messages
        static ConcurrentBag<Context> clients;
        static ConcurrentQueue<string> messages;

        static SocketManager()
        {
            clients = new ConcurrentBag<Context>();
            messages = new ConcurrentQueue<string>();
        }

        //Display the main page of our web-app
        [Route("/")]
        public FluidTemplate Index()
        {
            return new FluidTemplate("./UI/index.html");
        }

        // On "/channel" uri the application server will call this method
        [Route("/channel")]
        public void Channel()
        {
            //Save the current client into the recipients list
            clients.Add(Context);

            while (true)
            {
                try
                {
                    // Take the message from the client
                    messages.Enqueue(Context.Reader.ReadLine());

                    // Store just the last six messages of clients
                    if (messages.Count>6)
                    {
                        string trashMessage;
                        messages.TryDequeue(out trashMessage);
                    }

                    foreach (var client in clients)
                    {
                        //join message build the message table for clients
                        client.Writer.WriteLine(string.Join("",messages));
                        client.Writer.Flush();
                    }
                }
                catch (Exception)
                {
                    //Client goes timeout
                    break;
                }
            }

            //The client has close the connection, so we remove it from the list
            var trash = Context;
            clients.TryTake(out trash);
        }
    }
}
