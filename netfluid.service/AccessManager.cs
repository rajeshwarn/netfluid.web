namespace NetFluid.Service
{
    public class AccessManager : FluidPage
    {
        static AccessManager()
        {
            Engine.SetController(Context =>
            {
                if (!Context.IsLocal)
                {
                    return new FluidTemplate("./UI/index.html");
                }
                return null;
            });
        }

        [Route("/")]
        public IResponse Index()
        {
            return new FluidTemplate("./UI/admin.html");
        }
    }
}
