using System;
using System.Runtime.Caching;

namespace NetFluid.Collections
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
        public Func<string, T> Get;

        /// <summary>
        /// Object cache expiration
        /// </summary>
        public TimeSpan Expiration;

        public AutoCache()
        {
            cache = MemoryCache.Default;
        }

        public T this [string index]
        {
            get
            {
                var o = cache[index];

                if (o != null) return o as T;

                o = Get(index);

                if (o!=null)
                    cache.Set(index,o,DateTimeOffset.Now+Expiration);
                return o as T;
            }
            set
            {
                cache.Set(index,value,DateTimeOffset.Now+Expiration);
            }
        }
    }
}
