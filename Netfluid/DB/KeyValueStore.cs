﻿using Netfluid.Collections;
using System;
using System.Threading.Tasks;

namespace Netfluid.DB
{
    public class KeyValueStore<T>
    {
        DiskCollection disk;
        ByteCache<string> cache;

        public KeyValueStore(string path)
        {
            disk = new DiskCollection(path);
            cache = new ByteCache<string>();
            cache.Load = id=> disk.Get(id);

            cache.OnRemove += (k, v) => disk.Replace(k, v);
        }

        public long MemoryLimit => cache.MemoryLimit;

        public void Insert(string key,T value)
        {
            var bytes = BSON.Serialize(value);
            cache.AddOrUpdate(key, bytes, 10000);

            Task.Factory.StartNew(()=>disk.Insert(key, BSON.Serialize(value)));
        }

        public T Get(string id)
        {
            var f = cache.Get(id);

            if (f != null)
                return BSON.Deserialize<T>(f);

            return default(T);
        }

        public void Update(string key,T value)
        {
            var bytes = BSON.Serialize(value);
            cache.AddOrUpdate(key, bytes, 10000);

            Task.Factory.StartNew(() => disk.Replace(key, BSON.Serialize(value)));
        }

        public void Delete(string key)
        {
            cache.Remove(key);
            Task.Factory.StartNew(() => disk.Delete(key));
        }

        public void ForEach(Action<T> act)
        {
            disk.ForEach(x => act(Get(x)));
        }
    }
}