using NetFluid.Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3.UserStat
{
    public class Report
    {
        public int UniqueVisitors;
        public float BounceFrequency;
        public IEnumerable<Tuple<string,int>> Referrer;
        public IEnumerable<Tuple<string, int>> Languages;
        public IEnumerable<string> Countries;
        public long ServedRequest;
        
        public Report(IEnumerable<UserData> data)
        {
            UniqueVisitors = data.Count();
            BounceFrequency = ((float)data.Count(x => x.Interactions.Count == 1) / (float)UniqueVisitors);
            Referrer = data.Select(x => x.Referer)
                       .Where(x => x != null)
                       .Distinct()
                       .Select(x => new Tuple<string,int>(x,data.Count(y=>y.Referer==x)))
                       .OrderByDescending(x=>x.Item2);

            var langs = data.SelectMany(x => x.Languages);

            Languages = langs.Distinct()
                         .Select(x => new Tuple<string, int>(x, langs.Count(y => y == x)))
                         .OrderByDescending(x => x.Item2);

            //GeoIP.GetCountry("12.0.0.1");

            ServedRequest = data.SelectMany(x=>x.Interactions).LongCount();
        }
    }
}
