using NetFluid.Mongo;
using NetFluid.Responses;

namespace NetFluid.Service
{
    [Route("cdn")]
    public class CDNManager:FluidPage
    {
        public static Repository<CDN> CDN { get; private set;  }

        static CDNManager()
        {
            CDN = new Repository<CDN>("mongodb://localhost", "NetFluidService");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<CDN>();
            CDN.Save(h);

            Engine.AddPublicFolder(h.Host,"/",h.Path);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            CDN.Remove(CDN[id]);
            return new RedirectResponse("/");
        }
    }
}
