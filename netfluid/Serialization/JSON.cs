using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFluid.Serialization
{
    public static class JSON
    {
        public static string Serialize(object obj)
        { 
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static dynamic Deserialize(string json)
        { 
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        }

        public static dynamic Deserialize<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}
