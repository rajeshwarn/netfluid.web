using NetFluid.Responses;

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

            HostRepository.Start(id);
            return new RedirectResponse("/");
        }
        
        [ParametrizedRoute("/stop")]
        public IResponse Stop(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            HostRepository.Stop(id);
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ReStart(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            HostRepository.ReStart(id);
            return new RedirectResponse("/");
        }

        [ParametrizedRoute("/delete")]
        public IResponse Delete(string id)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            HostRepository.Delete(id);
            return new RedirectResponse("/");
        }

        [Route("/update")]
        public IResponse Update(string id, string name, string application, string hosts, string endpoint, bool enabled, string username, string password)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            HostRepository.Update(id, name, application, hosts, endpoint, enabled, username, password);
            return new RedirectResponse("/");
        }

        [Route("/add")]
        public IResponse Update(string name, string application, string hosts, string endpoint, bool enabled, string username, string password)
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            HostRepository.Add(name, application, hosts, endpoint, enabled, username, password);
            return new RedirectResponse("/");
        }
    }
}
