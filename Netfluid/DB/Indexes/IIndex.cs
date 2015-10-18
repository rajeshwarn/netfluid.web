using System;
using System.Collections.Generic;

namespace Netfluid.DB
{
    public interface IIndex<T>
    {
        long Count { get; }
        string First { get; }
        string Last { get; }

        void Delete(string id);
        void ForEach(Action<T> act);
        string Get(T id);
        IEnumerable<string> GetId(int from = 0, int take = 1000);
        void Insert(T id, string to);
        T Pop();
        string Push(byte[] obj);
        void Replace(string id, byte[] obj);
    }
}