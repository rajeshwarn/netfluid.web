using System;
using System.Diagnostics;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Serializable]
    public class Host : MongoObject
    {
        public string Id { get; set; }

        public string Application;
        public bool Enabled;
        public string EndPoint;
        public string[] Hosts;
        public string Name;

        public string Password;
        public string Username;

        public Process Start()
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = "FluidPlayer.exe",
                    Arguments = Application,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };

                if (Username != null && Password != null)
                {
                    info.UserName = Username;
                    info.Password = Security.Secure(Password);
                }

                Process process = Process.Start(info);

                process.Exited += (x, y) =>
                {
                    if (!Enabled) return;
                    Engine.Logger.Log(LogLevel.Error, "Host " + Name + " unexpected termination, restarting");
                    Start();
                };
                return process;
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
}