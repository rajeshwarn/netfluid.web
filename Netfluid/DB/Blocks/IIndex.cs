using System;
using System.Collections.Generic;

namespace Netfluid.DB
{
	internal interface IIndex<K, V>
	{
        IEnumerable<Tuple<K, V>> All { get; }

        bool Delete(K key);
        bool Delete(K key, V value, IComparer<V> valueComparer = null);
        IEnumerable<Tuple<K, V>> EqualTo(K key);
        Tuple<K, V> Get(K key);
        void Insert(K key, V value);
        IEnumerable<Tuple<K, V>> LargerThan(K key);
        IEnumerable<Tuple<K, V>> LargerThanOrEqualTo(K key);
        IEnumerable<Tuple<K, V>> LessThan(K key);
        IEnumerable<Tuple<K, V>> LessThanOrEqualTo(K key);
    }
}

