using System;
using System.Collections.Generic;

namespace Netfluid.DB
{
    public interface IKeyValueStore<T>
    {
        long Count { get; }

        bool Any();
        void Delete(string key);
        bool Exists(string id);
        void ForEach(Action<T> act);
        T Get(string id);
        IEnumerable<string> GetId(int from = 0, int take = 1000);
        void Insert(string key, T value);
        void Update(string key, T value);
    }
}