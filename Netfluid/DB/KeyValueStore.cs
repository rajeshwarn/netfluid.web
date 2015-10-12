using Netfluid.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Netfluid.DB
{
    public class KeyValueStore<T>: IEnumerable<T> where T :new()
    {
        DiskCollection disk;
        ObjectCache<string,T> cache;
        Func<T, string> key;

        public KeyValueStore(string path,Func<T,string> keyselector)
        {
            disk = new DiskCollection(path);
            cache = new ObjectCache<string, T>();
            key = keyselector;
        }

        public void Insert(T value)
        {
            disk.Insert(key(value), BSON.Serialize(value));
        }

        public T Get(string id)
        {
            return BSON.Deserialize<T>(disk.Get(id));
        }

        public void Update(T obj)
        {
            disk.Replace(key(obj), BSON.Serialize<T>(obj));
        }

        public void Delete(T obj)
        {
            disk.Delete(key(obj));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return disk.All().Select(x=>BSON.Deserialize<T>(disk.Get(x))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return disk.All().Select(x => BSON.Deserialize<T>(disk.Get(x))).GetEnumerator();
        }
    }
}
