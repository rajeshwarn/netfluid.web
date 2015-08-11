using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace NetFluid
{
    /// <summary>
    /// System.Runtime.Caching.MemoryCache simplified
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoCache<T> where T : class
    {
        private readonly MemoryCache cache;

        /// <summary>
        /// If the requested object doesn't exist in cache retrieve it
        /// </summary>
        public Func<string, T> Get { get; set; }

        /// <summary>
        /// Called when an item is removed from the cache
        /// </summary>
        public Action<string, T> Expired { get; set; }

        /// <summary>
        /// Object cache expiration
        /// </summary>
        public TimeSpan Expiration { get; set; }

        /// <summary>
        /// If true, when an item is taken from the cache, its expiration is delayed
        /// </summary>
        public bool AutoRenew { get; set; }

        public IEnumerable<KeyValuePair<string, T>> Values
        {
            get
            {
                var c = cache.GetValues(null).ToArray();
                return c.Select(x=>new KeyValuePair<string,T>(x.Key,(T)x.Value));
            }
        }

        public AutoCache()
        {
            cache = MemoryCache.Default;
        }

        public void Remove(string id)
        {
            cache.Remove(id);
        }

        public void Clear()
        {
            Values.ToArray().ForEach(x => Remove(x.Key));
        }

        public T this [string index]
        {
            get
            {
                var o = cache[index];

                if (o != null) return o as T;

                o = Get(index);

                if (AutoRenew && o!=null)
                    cache.Set(index, o, new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now + Expiration,
                        RemovedCallback = (x =>
                        {
                            if (Expired != null) Expired(x.CacheItem.Key, x.CacheItem.Value as T);
                        })
                    });
                return o as T;
            }
            set
            {
                cache.Set(index, value, new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now + Expiration,
                    RemovedCallback = (x =>
                    {
                        if (Expired != null) Expired(x.CacheItem.Key, x.CacheItem.Value as T);
                    })
                });
            }
        }
    }
}
