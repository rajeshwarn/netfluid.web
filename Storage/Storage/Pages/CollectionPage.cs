using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FluidDB
{
    /// <summary>
    /// Represents the collection page AND a collection item, because CollectionPage represent a Collection (1 page = 1 collection). All collections pages are linked with Prev/Next links
    /// </summary>
    internal class CollectionPage : BasePage
    {
        public const int MAX_COLLECTIONS = 256;
        public const string NAME_PATTERN = @"^\w{1,30}$";

        /// <summary>
        /// Name of collection
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Get a reference for the free list data page - its private list per collection - each DataPage contains only data for 1 collection (no mixing)
        /// Must to be a Field to be used as parameter reference
        /// </summary>
        public uint FreeDataPageID;

        /// <summary>
        /// Get the number of documents inside this collection
        /// </summary>
        public uint DocumentCount { get; set; }

        /// <summary>
        /// Get all indexes from this collection
        /// </summary>
        public CollectionIndex[] Indexes { get; set; }

        /// <summary>
        /// Returns first free slot to be used 
        /// </summary>
        public byte GetFreeIndex()
        {
            for (byte i = 0; i < Indexes.Length; i++)
            {
                if (Indexes[i].IsEmpty) return i;
            }
            throw new LiteException("Collection " + CollectionName + " excceded the index limit: " + CollectionIndex.INDEX_PER_COLLECTION);
        }

        public CollectionIndex PK { get { return Indexes[0]; } }

        protected override void UpdateItemCount()
        {
            ItemCount = 1; // Fixed for CollectionPage
        }

        public CollectionPage()
            : base()
        {
            PageType = PageType.Collection;
            FreeDataPageID = uint.MaxValue;
            DocumentCount = 0;
            Indexes = new CollectionIndex[CollectionIndex.INDEX_PER_COLLECTION];
            FreeBytes = 0; // no free bytes on collection page: one collection per page

            for (var i = 0; i < Indexes.Length; i++)
            {
                Indexes[i] = new CollectionIndex() { Page = this };
            }
        }

        public override void ReadContent(BinaryReader reader)
        {
            CollectionName = reader.ReadString();
            FreeDataPageID = reader.ReadUInt32();
            DocumentCount = reader.ReadUInt32();

            foreach (var index in Indexes)
            {
                index.Field = reader.ReadString();
                index.Unique = reader.ReadBoolean();
                index.HeadNode = reader.ReadPageAddress();
                index.FreeIndexPageID = reader.ReadUInt32();
            }
        }

        public override void WriteContent(BinaryWriter writer)
        {
            writer.Write(CollectionName);
            writer.Write(FreeDataPageID);
            writer.Write(DocumentCount);

            foreach (var index in Indexes)
            {
                writer.Write(index.Field);
                writer.Write(index.Unique);
                writer.Write(index.HeadNode);
                writer.Write(index.FreeIndexPageID);
            }
        }
    }
}
