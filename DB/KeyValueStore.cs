using Netfluid.Json;
using Netfluid.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Netfluid.DB
{
    public class KeyValueStore<T> : IKeyValueStore<T>
    {
        class Slot
        {
            public DateTime Timestamp;
            public T Value;

            public Slot(T value)
            {
                Value = value;
                Timestamp = DateTime.Now;
            }
        }

        static JsonSerializerSettings settings;
        BinaryKeyValueStore disk;

        static KeyValueStore()
        {
            settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new IPAddressConverter() },
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public KeyValueStore(string path)
        {
            disk = new BinaryKeyValueStore(path);
        }

        public string Directory => disk.Directory;
        public string Name => disk.Name;
        public long Count => disk.Count;

        public virtual bool Any()
        {
            return Count != 0;
        }

        public virtual bool Exists(string id)
        {
            return disk.Exists(id);
        }

        public virtual void Insert(string key,T value)
        {
            var str = JSON.Serialize(new Slot(value), settings);
            var bytes = Encoding.UTF8.GetBytes(str);
            disk.Insert(key, bytes);
        }

        public virtual T Get(string id)
        {
            var f = disk.Get(id);

            if (f != null)
            {
                var json = Encoding.UTF8.GetString(f);
                var slot = JSON.Deserialize(json, settings) as Slot;
                return slot.Value;
            }

            return default(T);
        }

        public virtual void Update(string key,T value)
        {
            var str = JSON.Serialize(new Slot(value), settings);
            var bytes = Encoding.UTF8.GetBytes(str);

            disk.Replace(key, bytes);
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

        public virtual IEnumerable<T> ReadAll()
        {
            var list = new List<T>();
            ForEach(x=>list.Add(x));
            return list;
        }
    }
}
