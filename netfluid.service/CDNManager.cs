using System.IO;
using System.Linq;
using NetFluid.Mongo;
using NetFluid.Responses;

namespace NetFluid.Service
{
    [Route("cdn")]
    public class CDNManager:FluidPage
    {
        public static Repository<CDN> CDN { get; private set;  }

        public static void Start()
        {
            CDN = new Repository<CDN>("mongodb://localhost", "NetFluidService");

            if (!Directory.Exists("./CDN"))
                Directory.CreateDirectory("./CDN");

            CDN.ForEach(h=>Engine.AddPublicFolder(h.Host, "/", h.Path));
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<CDN>();
            CDN.Save(h);

            if (!Directory.Exists(h.Path))
                Directory.CreateDirectory(h.Path);

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
