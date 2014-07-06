using System.IO;
using NetFluid.Collections;

namespace NetFluid.Service
{
    [Route("/plugins")]
    public class PluginManager:FluidPage
    {
        public static XMLRepository<Plugin> Hosts { get; private set; }

        public override void OnLoad()
        {
            Hosts = new XMLRepository<Plugin>("plugin.xml");
            Hosts.ForEach(host => host.Hosts.ForEach(x => Engine.LoadHost(x, host.Application)));
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            Hosts.Remove(id);
            return new RedirectResponse("/");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<Plugin>();

            if (h.Id == null)
                h.Hosts.ForEach(x => Engine.LoadHost(x, h.Application));

            Hosts.Save(h);
            return new RedirectResponse("/");
        }
    }
}
