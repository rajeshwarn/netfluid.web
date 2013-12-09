using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Collections.Persistent
{
    [Serializable]
    public class PersistentDictionary<K, V> : IDictionary<K, V>
    {
        ConcurrentDictionary<K, V> dic;
        string path;

        public PersistentDictionary(string filename)
        {
            try
            {
                path = Path.GetFullPath(filename);

                if (File.Exists(path))
                    dic = Binary.Deserialize<ConcurrentDictionary<K, V>>(File.ReadAllBytes(path));
                else
                    dic = new ConcurrentDictionary<K, V>();
            }
            catch (Exception)
            {
                dic = new ConcurrentDictionary<K, V>();
            }
        }

        public void Add(K key, V value)
        {
            dic.AddOrUpdate(key, value, (x, y) => y);

            Task.Factory.StartNew(() =>
            {
                lock (dic)
                {
                    File.WriteAllBytes(path,Binary.Serialize(dic));
                }
            });
        }

        public bool ContainsKey(K key)
        {
            return dic.ContainsKey(key);
        }

        public ICollection<K> Keys
        {
            get { return dic.Keys; }
        }

        public bool Remove(K key)
        {
            V value;
            if (dic.TryRemove(key, out value))
            {
                Task.Factory.StartNew(() =>
                {
                    lock (dic)
                    {
                        File.WriteAllBytes(path, Binary.Serialize(dic));
                    }
                });
                return true;
            }
            return false;
        }

        public bool TryGetValue(K key, out V value)
        {
            return dic.TryGetValue(key, out value);
        }

        public ICollection<V> Values
        {
            get { return dic.Values; }
        }

        public V this[K key]
        {
            get
            {
                return dic[key];
            }
            set
            {
                Add(key,value);
            }
        }

        public void Add(KeyValuePair<K, V> item)
        {
            Add(item.Key,item.Value);
        }

        public void Clear()
        {
            dic.Clear();
            Task.Factory.StartNew(() =>
            {
                lock (dic)
                {
                    File.WriteAllBytes(path, Binary.Serialize(dic));
                }
            });
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return dic.Contains(item);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            dic.ToArray().CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dic.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            V trash;
            if (dic.TryRemove(item.Key, out trash))
            {
                Task.Factory.StartNew(() =>
                {
                    lock (dic)
                    {
                        File.WriteAllBytes(path, Binary.Serialize(dic));
                    }
                });
                return true;
            }
            return false;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return dic.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return dic.GetEnumerator();
        }
    }
}
