using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Netfluid.DB
{
	/// <summary>
	/// Record storage service that store data in form of records, each
	/// record made up from one or several blocks
	/// </summary>
	class RecordStorage
	{
		readonly BlockStorage storage;
        readonly ReaderWriterLockSlim storeLocker;

        const int MaxRecordSize = 4194304; // 4MB
		const int kNextBlockId = 0;
		const int kRecordLength = 1;
		const int kBlockContentLength = 2;
		const int kPreviousBlockId = 3;
		const int kIsDeleted = 4;

		//
		// Constructors
		//

		public RecordStorage (BlockStorage storage)
		{
            storeLocker = new ReaderWriterLockSlim();

            if (storage == null)
				throw new ArgumentNullException ("storage");

			this.storage = storage;

			if (storage.BlockHeaderSize < 48) {
				throw new ArgumentException ("Record storage needs at least 48 header bytes");
			}
		}

		//
		// Public Methods
		//

		public virtual byte[] Find (uint recordId)
		{
            storeLocker.EnterReadLock();
			// First grab the block
			using (var block = storage.Find (recordId))
			{
				if (block == null)
                {
                    storeLocker.ExitReadLock();
					return null;
				}

				// If this is a deleted block then ignore it
				if (1L == block.GetHeader(kIsDeleted))
                {
                    storeLocker.ExitReadLock();
                    return null;
				}

				// If this block is a child block then also ignore it
				if (0L != block.GetHeader (kPreviousBlockId))
                {
                    storeLocker.ExitReadLock();
                    return null;
				}

				// Grab total record size and allocate coressponded memory
				var totalRecordSize = block.GetHeader (kRecordLength);
				if (totalRecordSize > MaxRecordSize)
                {
                    storeLocker.ExitReadLock();
                    throw new NotSupportedException ("Unexpected record length: " + totalRecordSize);
				}
				var data = new byte[totalRecordSize];
				var bytesRead = 0;

				// Now start filling data
				Block currentBlock = block;
				while (true)
				{
					uint nextBlockId;

					using (currentBlock)
					{
						var thisBlockContentLength = currentBlock.GetHeader (kBlockContentLength);
						if (thisBlockContentLength > storage.BlockContentSize)
                        {
                            storeLocker.ExitReadLock();
                            throw new InvalidDataException ("Unexpected block content length: " + thisBlockContentLength);
						}

						// Read all available content of current block
						currentBlock.Read (dest:data, destOffset:bytesRead, srcOffset:0, count:(int)thisBlockContentLength);

						// Update number of bytes read
						bytesRead += (int)thisBlockContentLength;

						// Move to the next block if there is any
						nextBlockId = (uint)currentBlock.GetHeader (kNextBlockId);
						if (nextBlockId == 0)
                        {
                            storeLocker.ExitReadLock();
                            return data;
						}
					}// Using currentBlock

					currentBlock = this.storage.Find (nextBlockId);
					if (currentBlock == null)
                    {
                        storeLocker.ExitReadLock();
                        throw new InvalidDataException ("Block not found by id: " + nextBlockId);
					}
				}
			}
        }

		public virtual uint Create (Func<uint, byte[]> dataGenerator)
		{
			if (dataGenerator == null) {
				throw new ArgumentException ();
			}

            storeLocker.EnterWriteLock();
            using (var firstBlock = AllocateBlock ())
			{
				var returnId = firstBlock.Id;

				// Alright now begin writing data
				var data = dataGenerator (returnId);
				var dataWritten = 0;
				var dataTobeWritten = data.Length;
				firstBlock.SetHeader (kRecordLength, dataTobeWritten);

				// If no data tobe written,
				// return this block straight away
				if (dataTobeWritten == 0)
                {
                    storeLocker.ExitWriteLock();
					return returnId;
				}

				// Otherwise continue to write data until completion
				Block currentBlock = firstBlock;
				while (dataWritten < dataTobeWritten)
				{
					Block nextBlock = null;

					using (currentBlock)
					{
						// Write as much as possible to this block
						var thisWrite = (int)Math.Min (storage.BlockContentSize, dataTobeWritten -dataWritten);
						currentBlock.Write (data, dataWritten, 0, thisWrite);
						currentBlock.SetHeader (kBlockContentLength, (long)thisWrite);
						dataWritten += thisWrite;

						// If still there are data tobe written,
						// move to the next block
						if (dataWritten < dataTobeWritten) {
							nextBlock = AllocateBlock ();
							var success = false;
							try {
								nextBlock.SetHeader (kPreviousBlockId, currentBlock.Id);
								currentBlock.SetHeader (kNextBlockId, nextBlock.Id);
								success = true;
							} finally {
								if ((false == success) && (nextBlock != null)) {
									nextBlock.Dispose ();
									nextBlock = null;
								}
							}
						} else {
							break;
						}
					} // Using currentBlock

					// Move to the next block if possible
					if (nextBlock != null) {
						currentBlock = nextBlock;
					}
				}
                storeLocker.ExitWriteLock();
                // return id of the first block that got dequeued
                return returnId;
			}
        }

		public virtual uint Create (byte[] data)
		{
			if (data == null) {
				throw new ArgumentException ();
			}

			return Create (recordId => data);
		}

		public virtual uint Create ()
		{
            storeLocker.EnterWriteLock();
			using (var firstBlock = AllocateBlock ())
			{
                storeLocker.ExitWriteLock();
                return firstBlock.Id;
			}
		}

		public virtual void Delete (uint recordId)
		{
            storeLocker.EnterWriteLock();
			using (var block = storage.Find (recordId))
			{
				Block currentBlock = block;
				while (true)
				{
					Block nextBlock = null;

					using (currentBlock)
					{
						MarkAsFree (currentBlock.Id);
						currentBlock.SetHeader (kIsDeleted, 1L);

						var nextBlockId = (uint)currentBlock.GetHeader (kNextBlockId);
						if (nextBlockId == 0) {
							break;
						} else {
							nextBlock = storage.Find (nextBlockId);
							if (currentBlock == null)
                            {
                                storeLocker.ExitWriteLock();
                                throw new InvalidDataException ("Block not found by id: " + nextBlockId);
							}
						}
					}// Using currentBlock

					// Move to next block
					if (nextBlock != null) {
						currentBlock = nextBlock;
					}
				}
            }
            storeLocker.ExitWriteLock();
        }

		public virtual void Update (uint recordId, byte[] data)
		{
			var written = 0;
			var total = data.Length;
			var blocks = FindBlocks (recordId);
			var blocksUsed = 0;
			var previousBlock = (Block)null;

            storeLocker.EnterWriteLock();
            try {
				// Start writing block by block..
				while (written < total)
				{
					// Bytes to be written in this block
					var bytesToWrite = Math.Min (total-written, storage.BlockContentSize);

					// Get the block where the first byte of remaining data will be written to
					var blockIndex = (int)Math.Floor (written/(double)storage.BlockContentSize);

					// Find the block to write to:
					// If `blockIndex` exists in `blocks`, then write into it,
					// otherwise allocate a new one for writting
					var target = (Block)null;
					if (blockIndex < blocks.Count) {
						target = blocks[blockIndex];
					} else {
						target = AllocateBlock ();
						if (target == null)
                        {
                            storeLocker.ExitWriteLock();
                            throw new Exception ("Failed to allocate new block");
						}
						blocks.Add (target);
					} 

					// Link with previous block
					if (previousBlock != null) {
						previousBlock.SetHeader (kNextBlockId, target.Id);
						target.SetHeader (kPreviousBlockId, previousBlock.Id);
					}

					// Write data
					target.Write (src:data, srcOffset: written, dstOffset: 0, count: bytesToWrite);
					target.SetHeader (kBlockContentLength, bytesToWrite);
					target.SetHeader (kNextBlockId, 0);
					if (written == 0) {
						target.SetHeader (kRecordLength, total);
					}

					// Get ready fr next loop
					blocksUsed++;
					written += bytesToWrite;
					previousBlock = target;
				}

				// After writing, delete off any unused blocks
				if (blocksUsed < blocks.Count) {
					for (var i = blocksUsed; i < blocks.Count; i++) {
						MarkAsFree (blocks[i].Id);
					}
				}
			} finally {
				// Always dispose all fetched blocks after finish using them
				foreach (var block in blocks)
				{
					block.Dispose ();
				}
			}
            storeLocker.ExitWriteLock();
        }

		//
		// Private Methods
		//

		/// <summary>
		/// Find all blocks of given record, return these blocks in order.
		/// </summary>
		/// <param name="recordId">Record identifier.</param>
		List<Block> FindBlocks (uint recordId)
		{
			var blocks = new List<Block>();
			var success = false;

			try
            {
				var currentBlockId = recordId;

				do {
					// Grab next block
					var block = storage.Find (currentBlockId);
					if (null == block) {
						// Special case: if block #0 never created, then attempt to create it
						if (currentBlockId == 0) {
							block = storage.CreateNew ();
						} else
                        {
                            throw new Exception ("Block not found by id: " + currentBlockId);
						}
					}
					blocks.Add (block);

					// If this is a deleted block then ignore the fuck out of it
					if (1L == block.GetHeader(kIsDeleted))
                    {
                        throw new InvalidDataException ("Block not found: " + currentBlockId);
					}

					// Move next
					currentBlockId = (uint)block.GetHeader (kNextBlockId);
				} while (currentBlockId != 0);

				success = true;
                return blocks;
			} finally {
				// Incase shit happens, dispose all fetched blocks
				if (false == success) {
					foreach (var block in blocks) {
						block.Dispose ();
					}
				}
            }
        }

		/// <summary>
		/// Allocate new block for use, either by dequeueing an exising non-used block
		/// or creating a new one
		/// </summary>
		/// <returns>Newly allocated block ready to use.</returns>
		Block AllocateBlock ()
		{
			uint resuableBlockId;
			Block newBlock;

			if (false == TryFindFreeBlock (out resuableBlockId))
            {
				newBlock = storage.CreateNew ();

                if (newBlock == null)
                {
					throw new Exception ("Failed to create new block");
				}
			}
			else
            {
				newBlock = storage.Find (resuableBlockId);
				if (newBlock == null) {
					throw new InvalidDataException ("Block not found by id: " + resuableBlockId);
				}
				newBlock.SetHeader (kBlockContentLength, 0L);
				newBlock.SetHeader (kNextBlockId, 0L);
				newBlock.SetHeader (kPreviousBlockId, 0L);
				newBlock.SetHeader (kRecordLength, 0L);
				newBlock.SetHeader (kIsDeleted, 0L);
			}
			return newBlock;
		}

		bool TryFindFreeBlock (out uint blockId)
		{
			blockId = 0;
			Block lastBlock, secondLastBlock;
			GetSpaceTrackingBlock (out lastBlock, out secondLastBlock);

			using (lastBlock)
			using (secondLastBlock)
			{
				// If this block is empty, then goto previous block
				var currentBlockContentLength = lastBlock.GetHeader (kBlockContentLength);
				if (currentBlockContentLength == 0)
				{
					// If there is no previous block, return false to indicate we can't dequeu
					if (secondLastBlock == null) {
						return false;
					}

					// Dequeue an uint from previous block, then mark current block as free
					blockId = ReadUInt32FromTrailingContent (secondLastBlock);

					// Back off 4 bytes before calling AppendUInt32ToContent
					secondLastBlock.SetHeader (kBlockContentLength, secondLastBlock.GetHeader(kBlockContentLength) -4);
					AppendUInt32ToContent (secondLastBlock, lastBlock.Id);

					// Forward 4 bytes, as an uint32 has been written
					secondLastBlock.SetHeader (kBlockContentLength, secondLastBlock.GetHeader(kBlockContentLength) +4);
					secondLastBlock.SetHeader (kNextBlockId, 0);
					lastBlock.SetHeader (kPreviousBlockId, 0);

					// Indicate success
					return true;
				}
				// If this block is not empty then dequeue an UInt32 from it
				else {
					blockId = ReadUInt32FromTrailingContent (lastBlock);
					lastBlock.SetHeader (kBlockContentLength, currentBlockContentLength -4);

					// Indicate sucess
					return true;
				}
			}
		}

		void AppendUInt32ToContent (Block block, uint value)
		{
			var contentLength = block.GetHeader (kBlockContentLength);

			if ((contentLength % 4) != 0) {
				throw new DataMisalignedException ("Block content length not %4: " + contentLength);
			}

			block.Write (src: BitConverter.GetBytes(value), srcOffset: 0, dstOffset: (int)contentLength, count: 4);
		}

		uint ReadUInt32FromTrailingContent (Block block)
		{
			var buffer = new byte[4];
			var contentLength = block.GetHeader (kBlockContentLength);

			if ((contentLength % 4) != 0) {
				throw new DataMisalignedException ("Block content length not %4: " + contentLength);
			}

			if (contentLength == 0) {
				throw new InvalidDataException ("Trying to dequeue UInt32 from an empty block");
			}

			block.Read (dest: buffer, destOffset: 0, srcOffset: (int)contentLength -4, count: 4);
			return BitConverter.ToUInt32 (buffer);
		}

		void MarkAsFree (uint blockId)
		{
			Block lastBlock, secondLastBlock, targetBlock = null;
			GetSpaceTrackingBlock (out lastBlock, out secondLastBlock);

			using (lastBlock)
			using (secondLastBlock)
			{
				try {
					// Just append a number, if there is some space left
					var contentLength = lastBlock.GetHeader (kBlockContentLength);
					if ((contentLength + 4) <= storage.BlockContentSize) {
						targetBlock = lastBlock;
					}
					// No more fucking space left, allocate new block for writing.
					// Note that we allocate fresh new block, if we reuse it may fuck things up
					else {
						targetBlock = storage.CreateNew ();
						targetBlock.SetHeader (kPreviousBlockId, lastBlock.Id);

						lastBlock.SetHeader (kNextBlockId, targetBlock.Id);

						contentLength = 0;
					}

					// Write!
					AppendUInt32ToContent (targetBlock, blockId);

					// Extend the block length to 4, as we wrote a number
					targetBlock.SetHeader (kBlockContentLength, contentLength+4);
				} finally {
					// Always dispose targetBlock
					if (targetBlock != null)
					{
						targetBlock.Dispose ();
					}
				}
			}
		}

		/// <summary>
		/// Get the last 2 blocks from the free space tracking record, 
		/// </summary>
		void GetSpaceTrackingBlock (out Block lastBlock, out Block secondLastBlock)
		{
			lastBlock = null;
			secondLastBlock = null;

			// Grab all record 0's blocks
			var blocks = FindBlocks (0);

			try {
				if (blocks == null || (blocks.Count == 0)) {
					throw new Exception ("Failed to find blocks of record 0");
				}

				// Assign
				lastBlock = blocks[blocks.Count -1];
				if (blocks.Count > 1) {
					secondLastBlock = blocks[blocks.Count -2];
				}
			} finally {
				// Awlays dispose unused blocks
				if (blocks != null)
				{
					foreach (var block in blocks)
					{
						if ((lastBlock == null || block != lastBlock) 
							&& (secondLastBlock == null || block != secondLastBlock))
						{
							block.Dispose ();
						}
					}
				}
			}
		}
	}
}