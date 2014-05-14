﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Permissions;

namespace NetFluid
{
    public class QueryValueCollection : IEnumerable<QueryValue>
    {
        private readonly Dictionary<string, QueryValue> values;

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

        public QueryValue this[string name]
        {
            get
            {
                QueryValue q;
                if (values.TryGetValue(name, out q))
                {
                    return q;
                }
                return null;
            }
            set { values[name] = value; }
        }

        public IEnumerator<QueryValue> GetEnumerator()
        {
            return values.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.Values.GetEnumerator();
        }

        public bool Contains(string name)
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
                values.Add(key, new QueryValue(key, value));
        }

        public string ToJSON()
        {
            return "{" + string.Join(",", values.Values.Select(x => x.ToString())) + "}";
        }

        IEnumerator<QueryValue> IEnumerable<QueryValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            return values.GetHashCode();
        }

        public override string ToString()
        {
            return ToJSON();
        }

        /// <summary>
        /// Parse request values into specified object
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ToObject(Type type)
        {
            var obj = type.CreateIstance();

            foreach (var field in type.GetFields())
            {
                foreach (var name in values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, values[name].Parse(field.FieldType));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            foreach (var field in type.GetProperties())
            {
                foreach (var name in values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, values[name].Parse(field.PropertyType));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return obj;
        }


        /// <summary>
        /// Parse request values into specified object
        /// </summary>
        /// <returns></returns>
        public T ToObject<T>()
        {
            var type = typeof (T);
            var obj = type.CreateIstance();

            foreach (var field in type.GetFields())
            {
                foreach (var name in values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, values[name].Parse(field.FieldType));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            foreach (var field in type.GetProperties())
            {
                foreach (var name in values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, values[name].Parse(field.PropertyType));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return (T)obj;
        }
    }
}