using Netfluid.Json;
using Netfluid.Json.Converters;
using System;
using System.Collections.Generic;

namespace Netfluid.DB
{
    public class KeyValueStore<T> : IKeyValueStore<T>
    {
        DiskCollection disk;

        static KeyValueStore()
        {
            JSON.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new IPAddressConverter() }
            };
        }

        public KeyValueStore(string path)
        {
            disk = new DiskCollection(path);
        }

        public string Directory => disk.Directory;
        public string Name => disk.Name;
        public long Count => disk.Count;

        public virtual bool Any()
        {
            return Count != 0;
        }

        public virtual string Push(T obj)
        {
            return disk.Push(BSON.Serialize(obj));
        }

        public virtual T Pop()
        {
            var b = disk.Pop();
            if (b == null) return default(T);
            return BSON.Deserialize<T>(b);
        }

        public virtual bool Exists(string id)
        {
            return disk.Exists(id);
        }

        public virtual void Insert(string key,T value)
        {
            disk.Insert(key, BSON.Serialize(value));
        }

        public virtual T Get(string id)
        {
            var f = disk.Get(id);

            if (f != null)
                return BSON.Deserialize<T>(f);

            return default(T);
        }

        public virtual void Update(string key,T value)
        {
            disk.Replace(key, BSON.Serialize(value));
        }

        public virtual void Delete(string key)
        {
            disk.Delete(key);
        }

        public virtual IEnumerable<string> GetId(int from=0, int take=1000)
        {
            return disk.GetId(from, take);
        }

        public virtual void ForEach(Action<T> act)
        {
            disk.ForEach(x => act(Get(x)));
        }
    }
}
