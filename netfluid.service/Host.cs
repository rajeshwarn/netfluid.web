using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace NetFluid.Service
{
    [Serializable]
    public class Host
    {
        public string Name;
        public string Application;
        public List<string> Hosts;
        public string EndPoint;

        [NonSerialized]
        public Process Process;

        [NonSerialized]
        public bool Closing;

        public Host()
        {
            Closing = false;
        }

        public Host(string app, string name, string endpoint)
        {
            Application = Path.GetFullPath(app);
            Hosts = new List<string> {name};

            EndPoint = endpoint;
            Closing = false;
        }

        public void Start()
        {
            Process = Process.Start("FluidPlayer.exe", Application);
            Process.Exited += (x, y) =>
            {
                if (!Closing)
                {
                    Engine.Logger.Log(LogLevel.Error,"Host "+Name+" unexpected termination, restarting");
                    Process = Process.Start("FluidPlayer.exe", Application);
                }

            };
        }

        public void Kill()
        {
            Closing = true;
            Process.Kill();
        }
    }
}
