using System;
using System.Collections.Generic;
using System.IO;

namespace Netfluid.DB
{
	internal class BlockStorage
	{
		readonly Stream stream;

		public int DiskSectorSize { get; private set; }

		public int BlockSize { get; private set; }

        public int BlockHeaderSize { get; private set; }

        public int BlockContentSize { get; private set; }

        //
        // Constructors
        //
        public BlockStorage (Stream storage, int blockSize = 40960, int blockHeaderSize = 48)
		{
			if (storage == null)
				throw new ArgumentNullException ("storage");

			if (BlockHeaderSize >= BlockSize)
				throw new ArgumentException ("BlockHeaderSize cannot be larger than or equal to BlockSize");

			if (BlockSize < 128)
				throw new ArgumentException ("BlockSize too small");

			DiskSectorSize = ((BlockSize >= 4096) ? 4096 : 128);
			BlockSize = blockSize;
			BlockHeaderSize = blockHeaderSize;
			BlockContentSize = BlockSize - BlockHeaderSize;
			stream = storage;
		}

		//
		// Public Methods
		//

		public Block Find (uint blockId)
		{
            // First, move to that block.
            // If there is no such block return NULL
            var blockPosition = blockId * BlockSize;
			if ((blockPosition + BlockSize) > stream.Length) return null;

            // Read the first 4KB of the block to construct a block from it
            var firstSector = new byte[DiskSectorSize];
			stream.Position = blockId * BlockSize;
			stream.Read (firstSector, 0, DiskSectorSize);

			return new Block(this, blockId, firstSector, stream);
        }

		public Block CreateNew ()
		{
			if ((stream.Length % BlockSize) != 0)
				throw new DataMisalignedException ("Unexpected length of the stream: " + stream.Length);

			// Calculate new block id
			var blockId = (uint)Math.Ceiling (stream.Length / (double)BlockSize);

			// Extend length of underlying stream
			stream.SetLength ((blockId * BlockSize) + BlockSize);
			stream.Flush ();

			// Return desired block
			return new Block(this, blockId, new byte[DiskSectorSize], stream);
        }
	}
}
