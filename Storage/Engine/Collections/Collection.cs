using System;
using System.Collections.Generic;

namespace FluidDB
{
    public partial class Collection<T>
    {
        private uint _pageID;
        private FluidDatabase _engine;

        /// <summary>
        /// Get collection name
        /// </summary>
        public string Name { get; private set; }

        internal Collection(FluidDatabase engine, string name)
        {
            this.Name = name;
            _engine = engine;
            _pageID = uint.MaxValue;
        }

        /// <summary>
        /// Get the collection page only when nedded. Gets from cache always to garantee that wil be the last (in case of _clearCache will get a new one)
        /// </summary>
        internal CollectionPage GetCollectionPage(bool addIfNotExits)
        {
            // when a operation is read-only, request collection page without add new one
            // use this moment to check if data file was changed (if in transaction, do nothing)
            if (addIfNotExits == false)
            {
                _engine.Transaction.AvoidDirtyRead();
            }

            // _pageID never change, even if data file was changed
            if (_pageID == uint.MaxValue)
            {
                var col = _engine.Collections.Get(this.Name);

                if (col == null)
                {
                    // create a new collection only if 
                    if (addIfNotExits)
                    {
                        col = _engine.Collections.Add(this.Name);
                    }
                    else
                    {
                        return null;
                    }
                }

                _pageID = col.PageID;

                return col;
            }

            return _engine.Pager.GetPage<CollectionPage>(_pageID);
        }
    }
}
