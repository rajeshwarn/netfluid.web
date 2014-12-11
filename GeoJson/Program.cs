using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFluid;
namespace GeoJson
{
    class Program
    {
        static void Main(string[] args)
        {
            var dic = new Dictionary<string, List<string>>();
            foreach (var line in File.ReadAllLines("admin1CodesASCII.txt"))
            {
                var parts = line.Split('\t');
                var n = parts[0].Substring(0, parts[0].IndexOf('.'));

                if (!dic.ContainsKey(n))
                    dic.Add(n, new List<string>());

                dic[n].Add(parts[2]);
            }
            File.WriteAllText("OUPUT.JSON",dic.ToJSON());
        }
    }
}
