using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;

namespace FluidDB
{
    /// <summary>
    /// Represent all cache system and track dirty pages. All pages that load and need to be track for
    /// dirty (to be persist after) must be added in this class.
    /// </summary>
    internal class CacheService
    {
        MemoryCache cache;

        // a very simple dictionary for pages cache and track
        private Dictionary<uint, BasePage> _cache;

        private DiskService _disk;

        private HeaderPage _header;

        public CacheService(DiskService disk)
        {
            _disk = disk;
            _cache = new Dictionary<uint, BasePage>();

            var conf = new NameValueCollection();
            conf.Add("pollingInterval", "2");
            conf.Add("cacheMemoryLimitMegabytes","512");
            conf.Add("physicalMemoryLimitPercentage", "70");

            cache = new MemoryCache(Guid.NewGuid().ToString(), conf);
        }

        void AddToCache(BasePage page)
        {
            var policy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromSeconds(3),
                RemovedCallback = x =>
                {
                    var removedPage = x.CacheItem.Value as BasePage;
                    var removedKey = uint.Parse(x.CacheItem.Key);

                    if (removedPage.IsDirty)
                    {
                        _disk.WritePage(removedPage);
                    }
                }
            };
            cache.Add(page.PageID.ToString(), page, policy);
        }

        /// <summary>
        /// Gets total pages in cache
        /// </summary>
        public int PagesInCache { get { return cache.Count(); } }

        /// <summary>
        /// Get header page in cache or request for a new instance if not existis yet
        /// </summary>
        public HeaderPage Header
        {
            get
            {
                if (_header == null)
                    _header = _disk.ReadPage<HeaderPage>(0);
                return _header;
            }
        }

        /// <summary>
        /// Get a page inside cache system. Returns null if page not existis. 
        /// If T is more specific than page that I have in cache, returns null (eg. Page 2 is BasePage in cache and this method call for IndexPage PageId 2)
        /// </summary>
        public T GetPage<T>(uint pageID) where T : BasePage
        {
            var page = cache[pageID.ToString()];

            // if a need a specific page but has a BasePage, returns null
            if (page != null && page.GetType() == typeof(BasePage) && typeof(T) != typeof(BasePage))
            {
                return null;
            }

            return (T)page;
        }

        /// <summary>
        /// Add a page to cache. if this page is in cache, override (except if is basePage - in this case, copy header)
        /// </summary>
        public void AddPage(BasePage page)
        {
            AddToCache(page);
        }

        /// <summary>
        /// Removing a page from cache
        /// </summary>
        public void RemovePage(uint pageID)
        {
            cache.Remove(pageID.ToString());
        }

        /// <summary>
        /// Empty cache and header page
        /// </summary>
        public void Clear(HeaderPage newHeaderPage)
        {
            _header = newHeaderPage;

            foreach (var item in cache)
            {
                cache.Remove(item.Key);
            }
        }

        /// <summary>
        /// Remove from cache only extend pages - useful for FileStorage
        /// </summary>
        public void RemoveExtendPages()
        {
            var keys = cache.Select(x=>x.Value as BasePage).Where(x => x.PageType == PageType.Extend && x.IsDirty == false).Select(x => x.PageID).ToList();

            foreach (var key in keys)
            {
                cache.Remove(key.ToString());
            }
        }

        /// <summary>
        /// Persist all dirty pages
        /// </summary>
        public void PersistDirtyPages()
        {
            foreach (var page in this.GetDirtyPages())
            {
                _disk.WritePage(page);
            }
        }

        /// <summary>
        /// Checks if cache has dirty pages
        /// </summary>
        public bool HasDirtyPages()
        {
            return this.GetDirtyPages().FirstOrDefault() != null;
        }

        /// <summary>
        /// Returns all dirty pages including header page
        /// </summary>
        public IEnumerable<BasePage> GetDirtyPages()
        {
            if (this.Header.IsDirty)
            {
                yield return _header;
            }

            foreach (var page in cache.Select(x=>x.Value as BasePage).Where(x => x.IsDirty))
            {
                yield return page;
            }
        }
    }
}
