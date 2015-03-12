﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    internal class DataService
    {
        private DiskService _disk;
        private PageService _pager;
        private CacheService _cache;

        public DataService(DiskService disk, CacheService cache, PageService pager)
        {
            _disk = disk;
            _cache = cache;
            _pager = pager;
        }

        /// <summary>
        /// Insert data inside a datapage. Returns dataPageID that idicates the first page
        /// </summary>
        public DataBlock Insert(CollectionPage col, IndexKey key, byte[] data)
        {
            // need to extend (data is bigger than 1 page)
            var extend = (data.Length + key.Length + DataBlock.DATA_BLOCK_FIXED_SIZE) > BasePage.PAGE_AVAILABLE_BYTES;

            // if extend, just search for a page with BLOCK_SIZE avaiable
            var dataPage = _pager.GetFreePage<DataPage>(col.FreeDataPageID, extend ? DataBlock.DATA_BLOCK_FIXED_SIZE : key.Length + data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

            // create a new block with first empty index on DataPage
            var block = new DataBlock { Position = new PageAddress(dataPage.PageID, dataPage.DataBlocks.NextIndex()), Page = dataPage, Key = key };

            // if extend, store all bytes on extended page.
            if (extend)
            {
                var extendPage = _pager.NewPage<ExtendPage>();
                block.ExtendPageID = extendPage.PageID;
                this.StoreExtendData(extendPage, data);
            }
            else
            {
                block.Data = data;
            }

            // add dataBlock to this page
            dataPage.DataBlocks.Add(block.Position.Index, block);

            dataPage.IsDirty = true;

            // add/remove dataPage on freelist if has space
            _pager.AddOrRemoveToFreeList(dataPage.FreeBytes > BasePage.RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            col.DocumentCount++;

            col.IsDirty = true;

            return block;
        }

        /// <summary>
        /// Update data inside a datapage. If new data can be used in same datapage, just update. Otherside, copy content to a new ExtendedPage
        /// </summary>
        public DataBlock Update(CollectionPage col, PageAddress blockAddress, byte[] data)
        {
            var dataPage = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = dataPage.DataBlocks[blockAddress.Index];
            var extend = dataPage.FreeBytes + block.Data.Length - data.Length <= 0;

            // check if need to extend
            if (extend)
            {
                // clear my block data
                block.Data = new byte[0];

                // create (or get a existed) extendpage and store data there
                ExtendPage extendPage;

                if (block.ExtendPageID == uint.MaxValue)
                {
                    extendPage = _pager.NewPage<ExtendPage>();
                    block.ExtendPageID = extendPage.PageID;
                }
                else
                {
                    extendPage = _pager.GetPage<ExtendPage>(block.ExtendPageID);
                }

                StoreExtendData(extendPage, data);
            }
            else
            {
                // If no extends, just update data block
                block.Data = data;

                // If there was a extended bytes, delete
                if (block.ExtendPageID != uint.MaxValue)
                {
                    _pager.DeletePage(block.ExtendPageID, true);
                    block.ExtendPageID = uint.MaxValue;
                }
            }

            // Add/Remove dataPage on freelist if has space AND its on/off free list
            _pager.AddOrRemoveToFreeList(dataPage.FreeBytes > DataPage.RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            dataPage.IsDirty = true;

            return block;
        }

        /// <summary>
        /// Read all data from datafile using a pageID as reference. If data is not in DataPage, read from ExtendPage. If readExtendData = false, do not read extended data 
        /// </summary>
        public DataBlock Read(PageAddress blockAddress, bool readExtendData = true)
        {
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = page.DataBlocks[blockAddress.Index];

            // if there is a extend page, read bytes to block.Data
            if (readExtendData && block.ExtendPageID != uint.MaxValue)
            {
                block.Data = this.Read(block.ExtendPageID);
            }

            return block;
        }

        /// <summary>
        /// Read all data from a extended page with all subsequences pages if exits
        /// </summary>
        public byte[] Read(uint extendPageID)
        {
            // read all extended pages and build byte array
            using (var buffer = new MemoryStream())
            {
                foreach (var extendPage in _pager.GetSeqPages<ExtendPage>(extendPageID))
                {
                    buffer.Write(extendPage.Data, 0, extendPage.Data.Length);
                }

                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Delete one dataBlock
        /// </summary>
        public DataBlock Delete(CollectionPage col, PageAddress blockAddress)
        {
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = page.DataBlocks[blockAddress.Index];

            // if there a extended page, delete all
            if (block.ExtendPageID != uint.MaxValue)
            {
                _pager.DeletePage(block.ExtendPageID, true);
            }

            // delete block inside page
            page.DataBlocks.Remove(block.Position.Index);

            // if there is no more datablocks, lets delete the page
            if (page.DataBlocks.Count == 0)
            {
                // first, remove from free list
                _pager.AddOrRemoveToFreeList(false, page, col, ref col.FreeDataPageID);

                _pager.DeletePage(page.PageID, false);
            }
            else
            {
                // add or remove to free list
                _pager.AddOrRemoveToFreeList(page.FreeBytes > DataPage.RESERVED_BYTES, page, col, ref col.FreeDataPageID);
            }

            col.DocumentCount--;

            col.IsDirty = true;
            page.IsDirty = true;

            return block;
        }

        /// <summary>
        /// Store all bytes in one extended page. If excced, call again to new page and make than continuous
        /// </summary>
        public void StoreExtendData(ExtendPage page, byte[] data)
        {
            // if data length is less the page-size
            if (data.Length <= ExtendPage.PAGE_AVAILABLE_BYTES)
            {
                page.Data = data;

                // if this page contains more continuous pages delete them (its a update case)
                if (page.NextPageID != uint.MaxValue)
                {
                    // Delete nextpage and all nexts
                    _pager.DeletePage(page.NextPageID, true);

                    // set my page with no NextPageID
                    page.NextPageID = uint.MaxValue;
                }

                page.IsDirty = true;
            }
            else
            {
                // split data - insert first bytes in this page and call again to insert next data
                page.Data = data.Take(BasePage.PAGE_AVAILABLE_BYTES).ToArray();

                ExtendPage newPage;

                // if i have a continuous page, get it... or create a new one
                if (page.NextPageID != uint.MaxValue)
                    newPage = _pager.GetPage<ExtendPage>(page.NextPageID);
                else
                    newPage = _pager.NewPage<ExtendPage>(page);

                page.IsDirty = true;

                StoreExtendData(newPage, data, BasePage.PAGE_AVAILABLE_BYTES);
            }
        }

        public void StoreExtendData(ExtendPage page, byte[] data, int skip)
        {
            // if data length is less the page-size
            if ((data.Length - skip) <= BasePage.PAGE_AVAILABLE_BYTES)
            {
                var newArray = new byte[data.Length - skip];
                Array.Copy(data, skip, newArray, 0, newArray.Length);
                page.Data = newArray;

                // if this page contains more continuous pages delete them (its a update case)
                if (page.NextPageID != uint.MaxValue)
                {
                    // Delete nextpage and all nexts
                    _pager.DeletePage(page.NextPageID, true);

                    // set my page with no NextPageID
                    page.NextPageID = uint.MaxValue;
                }

                page.IsDirty = true;
            }
            else
            {
                // split data - insert first bytes in this page and call again to insert next data
                var newArray = new byte[BasePage.PAGE_AVAILABLE_BYTES];
                Array.Copy(data, newArray, BasePage.PAGE_AVAILABLE_BYTES);
                page.Data = newArray;

                ExtendPage newPage;

                // if i have a continuous page, get it... or create a new one
                if (page.NextPageID != uint.MaxValue)
                    newPage = _pager.GetPage<ExtendPage>(page.NextPageID);
                else
                    newPage = _pager.NewPage<ExtendPage>(page);

                page.IsDirty = true;

                StoreExtendData(newPage, data, skip + BasePage.PAGE_AVAILABLE_BYTES);
            }
        }
    }
}
