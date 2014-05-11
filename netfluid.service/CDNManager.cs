using MongoDB.Bson;
using NetFluid.Mongo;
using NetFluid.Responses;

namespace NetFluid.Service
{
    [Route("cdn")]
    class CDNManager:FluidPage
    {
        private static readonly Repository<CDN> hosts;

        static CDNManager()
        {
            hosts = new Repository<CDN>("mongodb://localhost", "NetFluidService");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Update()
        {
            var h = Request.Values.ToObject<CDN>();

            if (Request.Values.Contains("id"))
                h.Id = ObjectId.Parse(Request.Values["id"].Value);

            hosts.Save(h);

            return new RedirectResponse("/");
        }

        [ParametrizedRoute("delete")]
        public IResponse Delete(string id)
        {
            hosts.Remove(hosts[id]);
            return new RedirectResponse("/");
        }
    }
}
