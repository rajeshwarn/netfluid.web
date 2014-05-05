using System;
using System.IO;

namespace NetFluid.Service
{
    [Route("host")]
    public class HostManager:FluidPage
    {
        [Route("/")]
        public IResponse Home()
        {
            return new FluidTemplate(Context.IsLocal ? "./UI/admin.html": "./UI/index.html");
        }

        [ParametrizedRoute("/start")]
        public IResponse Start(string name)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.Start(name);
            return new FluidTemplate("./UI/admin.html");
        }
        
        [ParametrizedRoute("/stop")]
        public IResponse Stop(string name)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.Stop(name);
            return new FluidTemplate("./UI/admin.html");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ReStart(string name)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.ReStart(name);
            return new FluidTemplate("./UI/admin.html");
        }

        [Route("/update")]
        public IResponse Update(string id, string name, string application, string hosts, string endpoint, bool enabled)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Request.Values.ForEach(x=>Console.WriteLine(x.ToString()));
            return null;
        }
    }
}
