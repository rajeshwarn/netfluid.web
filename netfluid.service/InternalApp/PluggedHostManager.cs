using System;
using System.IO;
using System.Reflection;
using NetFluid.Mongo;

namespace NetFluid.Service
{
    [Route("/host-int")]
    public class PluggedHostManager:FluidPage
    {
        public static Repository<PluggedHost> Hosts { get; private set; }

        public override void OnLoad()
        {
            Hosts = new Repository<PluggedHost>("mongodb://localhost", "NetFluidService");

            if (!Directory.Exists("./Internal-App"))
                Directory.CreateDirectory("./Internal-App");

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
            var h = Request.Values.ToObject<PluggedHost>();

            if (h.Id==null)
            {
                h.Hosts.ForEach(x => Engine.LoadHost(x, h.Application));
            }

            Hosts.Save(h);
            return new RedirectResponse("/");
        }
    }
}
