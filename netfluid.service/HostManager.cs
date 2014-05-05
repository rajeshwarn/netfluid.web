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
        public IResponse Start(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.Start(id);
            return new FluidTemplate("./UI/admin.html");
        }
        
        [ParametrizedRoute("/stop")]
        public IResponse Stop(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.Stop(id);
            return new FluidTemplate("./UI/admin.html");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ReStart(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.ReStart(id);
            return new FluidTemplate("./UI/admin.html");
        }

        [Route("/update")]
        public IResponse Update(string id, string name, string application, string hosts, string endpoint, bool enabled)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");
            
            Service.Update(id, name, application, hosts, endpoint, enabled);
            return new FluidTemplate("./UI/admin.html");
        }

        [Route("/add")]
        public IResponse Update(string name, string application, string hosts, string endpoint, bool enabled)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            Service.Add(name, application, hosts, endpoint, enabled);
            return new FluidTemplate("./UI/admin.html");
        }
    }
}
