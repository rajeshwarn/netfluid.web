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

        public long Count => disk.Count;

        public bool Any()
        {
            return Count != 0;
        }

        public void Insert(string key,T value)
        {
            disk.Insert(key, BSON.Serialize(value));
        }

        public T Get(string id)
        {
            var f = disk.Get(id);

            if (f != null)
                return BSON.Deserialize<T>(f);

            return default(T);
        }

        public void Update(string key,T value)
        {
            disk.Replace(key, BSON.Serialize(value));
        }

        public void Delete(string key)
        {
            disk.Delete(key);
        }

        public IEnumerable<string> GetId(int from=0, int take=1000)
        {
            return disk.GetId(from, take);
        }

        public void ForEach(Action<T> act)
        {
            disk.ForEach(x => act(Get(x)));
        }
    }
}
