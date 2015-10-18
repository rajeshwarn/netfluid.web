﻿using Netfluid.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Netfluid.DB
{
    public class KeyValueStore : IKeyValueStore<object>
    {
        class Slot
        {
            public Type Type;
            public byte[] Payload;
        }

        KeyValueStore<Slot> store;

        public KeyValueStore(string path)
        {
            store = new KeyValueStore<Slot>(path);
        }

        public long Count => store.Count;

        public bool Any()
        {
            return Count != 0;
        }

        public void Insert(string key,object value)
        {
            var slot = new Slot
            {
                Type = value.GetType(),
                Payload = BSON.Serialize(value)
            };

            store.Insert(key, slot);
        }

        public object Get(string id)
        {
            var s = store.Get(id);

            if (s != null)
                return BSON.Deserialize(s.Payload,s.Type);

            return null;
        }

        public void Update(string key,object value)
        {
            var slot = new Slot
            {
                Type = value.GetType(),
                Payload = BSON.Serialize(value)
            };

            store.Update(key,slot);
        }

        public void Delete(string key)
        {
            store.Delete(key);
        }

        public IEnumerable<string> GetId(int from=0, int take=1000)
        {
            return store.GetId(from,take);
        }

        public void ForEach(Action<object> act)
        {
            store.ForEach(x=> 
            {
                act(BSON.Deserialize(x.Payload,x.Type));
            });
        }
    }
}