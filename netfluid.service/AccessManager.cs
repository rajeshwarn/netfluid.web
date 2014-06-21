namespace NetFluid.Service
{
    public class AccessManager : FluidPage
    {
        static AccessManager()
        {
            Engine.SetController(Context =>
            {
                return !Context.IsLocal ? new FluidTemplate("./UI/index.html") : null;
            });
        }

        [Route("/")]
        public IResponse Index()
        {
            return new FluidTemplate("./UI/admin.html");
        }
    }
}
