﻿using System.Collections.Generic;
using System.Linq;

namespace NetFluid
{

    public class QueryValueCollection : IEnumerable<QueryValue>
    {
        readonly Dictionary<string, QueryValue> values;

        public QueryValueCollection()
        {
            values = new Dictionary<string, QueryValue>();
        }

        public QueryValueCollection(IEnumerable<QueryValue> collection)
        {
            values = new Dictionary<string, QueryValue>();

            foreach (var item in collection)
            {
                Add(item.Name, item);
            }
        }

        public bool Defines(string name)
        {
            return values.ContainsKey(name);
        }

        public void Add(string key, QueryValue value)
        {
            if (values.ContainsKey(key))
                values[key].Add(value);
            else
                values.Add(key, value);
        }

        public void Add(string key, string value)
        {
            if (values.ContainsKey(key))
                values[key].Add(value);
            else
                values.Add(key, new QueryValue(key,value));
        }

        public string ToJSON()
        {
            return "{" + string.Join(",",values.Values.Select(x=>x.ToString())) +"}";
        }

        public QueryValue this[string name]
        {
            get 
            {
                QueryValue q;
                if (values.TryGetValue(name,out q))
                {
                    return q;
                }
                return null;
            }
            set 
            {
                values[name] = value;
            }
        }

        public IEnumerator<QueryValue> GetEnumerator()
        {
            return values.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return values.Values.GetEnumerator();
        }
    }
}
