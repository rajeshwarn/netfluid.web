using NetFluid.Collections;

namespace NetFluid.Service.Forwarding
{
    [Route("forwarding")]
    public class ForwardingManager:FluidPage
    {
        public static readonly IRepository<Forwarding> Forwarded;

        static ForwardingManager()
        {
            Forwarded = new XMLRepository<Forwarding>("forwarding.xml");
            Forwarded.ForEach(fow =>
            {
                if (fow.Enabled)
                    fow.Hosts.ForEach(hostname => Engine.Cluster.AddFowarding(hostname,fow.EndPoint));
            });
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            Forwarded.Remove(id);
            return new RedirectResponse("/");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<Forwarding>();

            if (h.Enabled)
                h.Hosts.ForEach(vhost => Engine.Cluster.AddFowarding(vhost, h.EndPoint));
            else
                h.Hosts.ForEach(vhost => Engine.Cluster.RemoveFowarding(vhost));

            Forwarded.Save(h);
            return new RedirectResponse("/");
        }
    }
}
