using System.Collections.Generic;
using System.Linq;
using NetFluid.Collections;

namespace NetFluid.Service
{
    public class CDNRepository
    {
        static readonly XMLRepository<CDN> cdn;

        static CDNRepository()
        {
            cdn = new XMLRepository<CDN>("cdn.xml");
        }

        public static IEnumerable<CDN> CDN
        {
            get { return cdn; }
        }

        public static CDN Get(string id)
        {
            return cdn.FirstOrDefault(x => x.Id == id);
        }

        public static void Update(string id, string host, string path)
        {
            var c = Get(id);

            if (c!=null)
            {
                c.Host = host;
                c.Path = path;
            }
            cdn.Save();
        }

        public static void Add(string host, string path)
        {
            cdn.Save(new CDN
            {
                Id = Security.UID(),
                Host = host,
                Path = path
            });

            Engine.AddPublicFolder(host,"/",path,false);
        }

        public static void Delete(string id)
        {
            cdn.Remove(x=>x.Id == id);
        }
    }
}
