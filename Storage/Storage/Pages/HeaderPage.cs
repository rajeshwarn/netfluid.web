﻿using System.IO;

namespace FluidDB
{
    internal class HeaderPage : BasePage
    {
        /// <summary>
        /// Header info the validate that datafile is a LiteDB file
        /// </summary>
        private const string HEADER_INFO = "** This is a LiteDB file **";

        /// <summary>
        /// Datafile specification version
        /// </summary>
        private const byte FILE_VERSION = 2;

        /// <summary>
        /// Get/Set the changeID of data. When a client read pages, all pages are in the same version. But when OpenTransaction, we need validade that current changeID is the sabe that we have in cache
        /// </summary>
        public ushort ChangeID { get; set; }

        /// <summary>
        /// Get/Set the pageID that start sequenece with a complete empty pages (can be used as a new page)
        /// </summary>
        public uint FreeEmptyPageID;

        /// <summary>
        /// Last created page - Used when there is no free page inside file
        /// </summary>
        public uint LastPageID { get; set; }

        /// <summary>
        /// Get/Set a user version of database file
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// Get/Set max datafile size
        /// </summary>
        public long MaxFileLength { get; set; }

        /// <summary>
        /// Get max page ID for this datafile
        /// </summary>
        internal uint MaxPageID
        {
            get { return MaxFileLength == long.MaxValue ? uint.MaxValue : (uint)(this.MaxFileLength / BasePage.PAGE_SIZE); }
        }

        public HeaderPage()
            : base()
        {
            PageID = 0;
            this.PageType = FluidDB.PageType.Header;
            this.FreeEmptyPageID = uint.MaxValue;
            this.ChangeID = 0;
            this.LastPageID = 0;
            this.UserVersion = 0;
            this.MaxFileLength = long.MaxValue;
            this.FreeBytes = 0; // no free bytes on header
        }

        public override void ReadHeader(BinaryReader reader)
        {
            reader.Seek(4); // skip byte 0 - it's loked in a transaction
            //this.PageID = reader.ReadUInt32();
            this.PrevPageID = reader.ReadUInt32();
            this.NextPageID = reader.ReadUInt32();
            this.PageType = (PageType)reader.ReadByte();
            this.ItemCount = reader.ReadUInt16();
            this.FreeBytes = reader.ReadInt32();
        }

        public override void ReadContent(BinaryReader reader)
        {
            var info = reader.ReadString(BasePage.PAGE_HEADER_SIZE);

            if (info != HEADER_INFO)
                throw new LiteException("This file is not a LiteDB datafile");

            if (reader.ReadByte() != FILE_VERSION)
                throw new LiteException("Invalid LiteDB datafile version");

            this.ChangeID = reader.ReadUInt16();
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.UserVersion = reader.ReadInt32();
            this.MaxFileLength = reader.ReadInt64();
        }

        public override void WriteContent(BinaryWriter writer)
        {
            writer.Write(HEADER_INFO, BasePage.PAGE_HEADER_SIZE);
            writer.Write(FILE_VERSION);
            writer.Write(this.ChangeID);
            writer.Write(this.FreeEmptyPageID);
            writer.Write(this.LastPageID);
            writer.Write(this.UserVersion);
            writer.Write(this.MaxFileLength);
        }
    }
}
