namespace NetFluid.Service
{
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
            if (Context.IsLocal)
            {
                Service.Start(name);
                return new FluidTemplate("./UI/admin.html");
            }
            return new FluidTemplate("./UI/index.html");
        }
        [ParametrizedRoute("/stop")]
        public IResponse Stop(string name)
        {
            if (Context.IsLocal)
            {
                Service.Stop(name);
                return new FluidTemplate("./UI/admin.html");
            }
            return new FluidTemplate("./UI/index.html");
        }

        [ParametrizedRoute("/restart")]
        public IResponse ResStart(string name)
        {
            if (Context.IsLocal)
            {
                Service.ReStart(name);
                return new FluidTemplate("./UI/admin.html");
            }
            return new FluidTemplate("./UI/index.html");
        }
    }
}
