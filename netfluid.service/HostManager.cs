using MongoDB.Driver;

namespace NetFluid.Service
{
    [Route("host")]
    public class HostManager:FluidPage
    {
        private static readonly MongoDatabase database;

        public static MongoCollection<Host> Hosts
        {
            get { return database.GetCollection<Host>("Host"); }
        }

        static HostManager()
        {
            var client = new MongoClient("mongodb://localhost");
            database = client.GetServer().GetDatabase("NetFluidService");
        }

        [Route("/")]
        public IResponse Home()
        {
            if (!Context.IsLocal) return new FluidTemplate("./UI/index.html");

            var host = Context.Request.Values.ToObject<Host>(); 

            switch (Request.HttpMethod)
            {
                case "POST":
                    return Add(host);
                case "PUT":
                    return Update(host);
                case "DELETE":
                    return Delete(host);
            }
            return new FluidTemplate("./UI/admin.html");
        }

        public static IResponse Delete(Host host)
        {
            return new RedirectResponse("/");
        }

        public static IResponse Update(Host host)
        {
            return new RedirectResponse("/");
        }

        public static IResponse Add(Host host)
        {
            return new RedirectResponse("/");
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
    }
}
