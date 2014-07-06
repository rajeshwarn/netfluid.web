using NetFluid;

namespace RaspSite
{
    public class Performance:FluidPage
    {
        public static long Served;
        public static long Sum;
        public static long Min;
        public static long Max;

        public static long Avg
        {
            get { return Sum/Served; }
        }

        public static void LoadPerformance()
        {
            Served = 1;
            Min = long.MaxValue;
            Max = long.MinValue;

            Context.Profiling += (speed,host, uri) =>
            {
                Served++;
                Sum += speed;
                Min = speed < Min ? speed : Min;
                Max = speed > Max ? speed : Max;
            };
        }

        [Route("/performance")]
        public IResponse Index()
        {
            return new FluidTemplate("embed:NetFluid.Site.UI.performance.html");
        }
    }
}
