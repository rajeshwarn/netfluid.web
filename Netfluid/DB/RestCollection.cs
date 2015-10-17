using Netfluid.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.DB
{
    class RestCollection
    {
        KeyValueStore<JObject> collection;

        public RestCollection(string path)
        {
            collection = new KeyValueStore<JObject>(path);
        }

        public void Mount(string baseUrl,NetfluidHost host)
        {
            host.Routes["GET", baseUrl] = new Route(new Func<int,int,IEnumerable<string>>((from,take)=> 
            {
                if (take > 2000) take = 2000;
                return collection.GetId(from,take);
            }));

            host.Routes["POST", baseUrl+"/:id"] = new Route(new Action<string,Context>((id,cnt) =>
            {
                var obj = JSON.Deserialize(cnt.Reader) as JObject;
                collection.Insert(id,obj);
            }));

            host.Routes["PUT", baseUrl + "/:id"] = new Route(new Action<string, Context>((id, cnt) =>
            {
                var obj = JSON.Deserialize(cnt.Reader) as JObject;
                collection.Update(id, obj);
            }));

            host.Routes["DELETE", baseUrl + "/:id"] = new Route(new Action<string, Context>((id, cnt) =>
            {
                collection.Delete(id);
            }));
        }
    }
}
