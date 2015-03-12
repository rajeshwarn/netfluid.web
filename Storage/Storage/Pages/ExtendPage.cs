﻿using System;
using System.IO;

namespace FluidDB
{
    /// <summary>
    /// Represent a extra data page that contains the object when is not possible store in DataPage (bigger then  PAGE_SIZE or on update has no more space on page)
    /// Can be used in sequence of pages to store big objects
    /// </summary>
    internal class ExtendPage : BasePage
    {
        /// <summary>
        /// Represent the part or full of the object - if this page has NextPageID the object is bigger than this page
        /// </summary>
        public Byte[] Data { get; set; }

        public override int FreeBytes
        {
            get { return PAGE_AVAILABLE_BYTES - Data.Length; }
        }

        protected override void UpdateItemCount()
        {
            this.ItemCount = (ushort)Data.Length;
        }

        public override void Clear()
        {
            base.Clear();
            this.Data = new byte[0];
        }

        public ExtendPage()
            : base()
        {
            this.PageType = FluidDB.PageType.Extend;
            this.Data = new byte[0];
        }

        public override void ReadContent(BinaryReader reader)
        {
            this.Data = reader.ReadBytes(this.ItemCount);
        }

        public override void WriteContent(BinaryWriter writer)
        {
            writer.Write(this.Data);
        }
    }
}
