using System.IO;
using NetFluid.Collections;

namespace NetFluid.Service
{
    [Route("cdn")]
    public class CDNManager:FluidPage
    {
        public static XMLRepository<CDN> CDN { get; private set;  }

        public override void OnLoad()
        {
            CDN = new XMLRepository<CDN>("cdn.xml");
            CDN.ForEach(h => Engine.Host(h.Host).PublicFolderManager.Add(h.Id, "/", h.Path));
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<CDN>();
            CDN.Save(h);

            if (!Directory.Exists(h.Path))
                Directory.CreateDirectory(h.Path);

            Engine.Host(h.Host).PublicFolderManager.Add(h.Id,"/",h.Path);

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
