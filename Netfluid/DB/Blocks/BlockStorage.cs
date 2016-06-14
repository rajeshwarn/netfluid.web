using System;
using System.Collections.Generic;
using System.IO;

namespace Netfluid.DB
{
	internal class BlockStorage
	{
		readonly Stream stream;
		readonly Dictionary<uint, Block> blocks = new Dictionary<uint, Block> ();

		public int DiskSectorSize { get; private set; }

		public int BlockSize { get; private set; }

        public int BlockHeaderSize { get; private set; }

        public int BlockContentSize { get; private set; }

        //
        // Constructors
        //

        public BlockStorage (Stream storage, int BlockSize = 40960, int BlockHeaderSize = 48)
		{
			if (storage == null)
				throw new ArgumentNullException ("storage");

			if (BlockHeaderSize >= BlockSize) {
				throw new ArgumentException ("BlockHeaderSize cannot be " +
					"larger than or equal " +
					"to " + "BlockSize");
			}

			if (BlockSize < 128) {
				throw new ArgumentException ("BlockSize too small");
			}

			this.DiskSectorSize = ((BlockSize >= 4096) ? 4096 : 128);
			this.BlockSize = BlockSize;
			this.BlockHeaderSize = BlockHeaderSize;
			this.BlockContentSize = BlockSize - BlockHeaderSize;
			this.stream = storage;
		}

		//
		// Public Methods
		//

		public Block Find (uint blockId)
		{
			// Check from initialized blocks
			if (true == blocks.ContainsKey(blockId))
			{
				return blocks[blockId];
			}

			// First, move to that block.
			// If there is no such block return NULL
			var blockPosition = blockId * BlockSize;
			if ((blockPosition + BlockSize) > this.stream.Length)
			{
				return null;
			}

			// Read the first 4KB of the block to construct a block from it
			var firstSector = new byte[DiskSectorSize];
			stream.Position = blockId * BlockSize;
			stream.Read (firstSector, 0, DiskSectorSize);

			var block = new Block (this, blockId, firstSector, this.stream);
			OnBlockInitialized (block);
			return block;
		}

		public Block CreateNew ()
		{
			if ((this.stream.Length % BlockSize) != 0) {
				throw new DataMisalignedException ("Unexpected length of the stream: " + this.stream.Length);
			}

			// Calculate new block id
			var blockId = (uint)Math.Ceiling ((double)this.stream.Length / (double)BlockSize);

			// Extend length of underlying stream
			this.stream.SetLength ((long)((blockId * BlockSize) + BlockSize));
			this.stream.Flush ();

			// Return desired block
			var block = new Block (this, blockId, new byte[DiskSectorSize], this.stream);
			OnBlockInitialized (block);
			return block;
		}

		//
		// Protected Methods
		//

		protected virtual void OnBlockInitialized (Block block)
		{
			// Keep reference to it
			blocks[block.Id] = block;

			// When block disposed, remove it from memory
			block.Disposed += HandleBlockDisposed;
		}

		protected virtual void HandleBlockDisposed (object sender, EventArgs e)
		{
			// Stop listening to it
			var block = (Block)sender;
			block.Disposed -= HandleBlockDisposed;

			// Remove it from memory
			blocks.Remove (block.Id);
		}
	}
}
