using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetFluid
{
    /// <summary>
    /// GET or POST values recieved from the client
    /// </summary>
    public class QueryValueCollection : IEnumerable<QueryValue>, IDisposable
    {
        private readonly Dictionary<string, QueryValue> _values;

        public QueryValueCollection()
        {
            _values = new Dictionary<string, QueryValue>();
        }

        public QueryValueCollection(IEnumerable<QueryValue> collection)
        {
            _values = new Dictionary<string, QueryValue>();

            foreach (var item in collection)
            {
                Add(item.Name, item);
            }
        }

        /// <summary>
        /// Retrive a query variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public QueryValue this[string name]
        {
            get
            {
                QueryValue q;
                return _values.TryGetValue(name, out q) ? q : null;
            }
            set { _values[name] = value; }
        }

        public T Parse<T>() where T :class
        {
            var type = typeof(T);
            var t = type.CreateIstance() as T;

            foreach (var key in _values.Keys)
            {
                var field = type.GetField(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(t, _values[key].Parse(field.FieldType));
                }

                var prop = type.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    prop.SetValue(t, _values[key].Parse(prop.PropertyType),null);
                }
            }

            return t;
        }


        /// <summary>
        /// List of variables
        /// </summary>
        /// <returns></returns>
        public IEnumerator<QueryValue> GetEnumerator()
        {
            return _values.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.Values.GetEnumerator();
        }

        /// <summary>
        /// True if this variable has been recieved
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return _values.ContainsKey(name);
        }

        /// <summary>
        /// Add a variable to the collection
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, QueryValue value)
        {
            if (_values.ContainsKey(key))
                _values[key].Add(value);
            else
                _values.Add(key, value);
        }

        /// <summary>
        /// Add avariable to the collection
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string value)
        {
            if (_values.ContainsKey(key))
                _values[key].Add(value);
            else
                _values.Add(key, new QueryValue(key, value));
        }

        /// <summary>
        /// JSON serialization of the collection
        /// </summary>
        /// <returns></returns>
        public string ToJSON()
        {
            return "{" + string.Join(",", _values.Values.Select(x => x.ToString())) + "}";
        }

        IEnumerator<QueryValue> IEnumerable<QueryValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            return _values.GetHashCode();
        }

        public override string ToString()
        {
            return ToJSON();
        }

        /// <summary>
        /// Parse request values into specified object type (see Netfluid parsing rules)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ToObject(Type type)
        {
            var obj = type.CreateIstance();

            foreach (var field in type.GetFields())
            {
                foreach (var name in _values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, _values[name].Parse(field.FieldType));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            foreach (var field in type.GetProperties())
            {
                foreach (var name in _values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, _values[name].Parse(field.PropertyType),null);
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
                foreach (var name in _values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, _values[name].Parse(field.FieldType));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            foreach (var field in type.GetProperties())
            {
                foreach (var name in _values.Keys.Where(name => String.Equals(name, field.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    try
                    {
                        field.SetValue(obj, _values[name].Parse(field.PropertyType),null);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return (T)obj;
        }

        public void Dispose()
        {
            _values.Clear();
        }
    }
}