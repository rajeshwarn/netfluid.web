using System;
using System.Collections.Generic;
using System.Threading;

namespace Netfluid.DB
{
	internal class Tree<K, V> : IIndex<K, V>
	{
		readonly ITreeNodeManager<K, V> nodeManager;
		readonly bool allowDuplicateKeys;
        ReaderWriterLockSlim locker;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sdb.BTree.Tree`2"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="nodeManager">Node manager.</param>
        public Tree (ITreeNodeManager<K, V> nodeManager, bool allowDuplicateKeys = false)
		{
			if (nodeManager == null)
				throw new ArgumentNullException ("nodeManager");
			this.nodeManager = nodeManager;
			this.allowDuplicateKeys = allowDuplicateKeys;

            locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		/// <summary>
		/// Delete specified entry
		/// </summary>
		public bool Delete (K key, V value, IComparer<V> valueComparer = null)
		{
			if (false == allowDuplicateKeys) {
				throw new InvalidOperationException ("This method should be called only from non-unique tree");
			}

			valueComparer = valueComparer == null ? Comparer<V>.Default : valueComparer;
			
			var deleted = false;
			var shouldContinue = true;

            locker.EnterWriteLock();
			try
            {
				while (shouldContinue)
				{
					// Iterating to find all entries we wish to delete
					using (var enumerator = (TreeEnumerator<K, V>)LargerThanOrEqualTo (key).GetEnumerator())
					{
						while (true)
						{
							// Stop enumerating as soon as we reached the end of the ebumerator
							if (false == enumerator.MoveNext()) {
								shouldContinue = false;
								break;
							}

							// Current entry
							var entry = enumerator.Current;

							// Stop searching as soon as we reach the bound,
							// where the larger key presents.
							if (nodeManager.KeyComparer.Compare(entry.Item1, key) > 0) {
								shouldContinue = false;
								break;
							}

							// If we reach an entry that match what requested, then delete it.
							if (valueComparer.Compare(entry.Item2, value) == 0)
							{
								enumerator.CurrentNode.Remove (enumerator.CurrentEntry);
								deleted = true;
								break; // Get new enumerator
							}
						}
					}
				}
			} catch (EndEnumeratingException) {
				
			}

			// Finalize stuff
			nodeManager.SaveChanges ();
            locker.ExitWriteLock();

			return deleted;
		}

		private class EndEnumeratingException : Exception { }

		/// <summary>
		/// Delete all entries of given key
		/// </summary>
		public bool Delete (K key)
		{
			if (true == allowDuplicateKeys) {
				throw new InvalidOperationException ("This method should be called only from unique tree");
			}

            locker.EnterReadLock();
			// Find the node tobe deleted using an enumerator
			using (var enumerator = (TreeEnumerator<K, V>)LargerThanOrEqualTo (key).GetEnumerator())
			{
				// If the first element of enumerator is the key we wishes to delete,
				// then tell the enumerator's current node to delete it.
				// Otherwise, consider the key client specified is not found.
				if (enumerator.MoveNext() && (nodeManager.KeyComparer.Compare (enumerator.Current.Item1, key) == 0))
				{
                    locker.EnterWriteLock();
					enumerator.CurrentNode.Remove (enumerator.CurrentEntry);
                    locker.ExitWriteLock();
                    locker.ExitReadLock();
					return true;
				}
			}

            locker.ExitReadLock();
			// Return false by default
			return false;
		}

		/// <summary>
		/// Insert an entry to the tree
		/// </summary>
		public void Insert (K key, V value)
		{
            locker.EnterReadLock();
			// First find the node where key should be inserted
			var insertionIndex = 0;
			var leafNode = FindNodeForInsertion (key, ref insertionIndex);

			// Duplication check
			if (insertionIndex >= 0 && false == allowDuplicateKeys)
            {
                locker.ExitReadLock();
				throw new TreeKeyExistsException (key);
			}

            locker.ExitReadLock();

            locker.EnterWriteLock();
			// Now insert to the leaf
			leafNode.InsertAsLeaf (key, value, insertionIndex >= 0 ? insertionIndex : ~insertionIndex);

			// If the leaf is overflow, then split it
			if (leafNode.IsOverflow) {
				TreeNode<K, V> left, right;
				leafNode.Split (out left, out right);
			}

			// Save changes, if any
			nodeManager.SaveChanges ();
            locker.ExitWriteLock();
		}

		/// <summary>
		/// Find entry by its key, this returns NULL when not foudn
		/// </summary>
		public Tuple<K, V> Get (K key)
		{
			var insertionIndex = 0;
            locker.EnterReadLock();

			var node = FindNodeForInsertion (key, ref insertionIndex);
			if (insertionIndex < 0)
            {
                locker.ExitReadLock();
				return null;
			}
            locker.ExitReadLock();
			return node.GetEntry (insertionIndex);
		}


        public IEnumerable<Tuple<K, V>> EqualTo(K key)
        {
            return System.Linq.Enumerable.Intersect(LargerThanOrEqualTo(key), LessThanOrEqualTo(key));
        }

        /// <summary>
        /// Search for all elements that larger than or equal to given key
        /// </summary>
        public IEnumerable<Tuple<K, V>> LargerThanOrEqualTo (K key)
		{
			var startIterationIndex = 0;

            locker.EnterReadLock();
			var node = FindNodeForIteration (key, this.nodeManager.RootNode, true, ref startIterationIndex);
            locker.ExitReadLock();

			return new TreeTraverser<K, V> (nodeManager
				, node
				, (startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex) -1
				, TreeTraverseDirection.Ascending);
		}


        /// <summary>
        /// Search for all elements that larger than given key
        /// </summary>
        public IEnumerable<Tuple<K, V>> All
        {
            get
            {
                var startIterationIndex = 0;
                return new TreeTraverser<K, V>(nodeManager
                    , nodeManager.RootNode
                    , (startIterationIndex >= 0 ? startIterationIndex : (~startIterationIndex - 1))
                    , TreeTraverseDirection.Ascending);
            }
        }

        /// <summary>
        /// Search for all elements that larger than given key
        /// </summary>
        public IEnumerable<Tuple<K, V>> LargerThan (K key)
		{
			var startIterationIndex = 0;

            locker.EnterReadLock();
			var node = FindNodeForIteration (key, this.nodeManager.RootNode, false, ref startIterationIndex);
            locker.ExitReadLock();

			return new TreeTraverser<K, V> (nodeManager
				, node
				, (startIterationIndex >= 0 ? startIterationIndex : (~startIterationIndex-1))
				, TreeTraverseDirection.Ascending);
		}

		/// <summary>
		/// Search for all elements that is less than or equal to given key
		/// </summary>
		public IEnumerable<Tuple<K, V>> LessThanOrEqualTo (K key)
		{
			var startIterationIndex = 0;
            locker.EnterReadLock();
			var node = FindNodeForIteration (key, this.nodeManager.RootNode, false, ref startIterationIndex);
            locker.ExitReadLock();

			return new TreeTraverser<K, V> (nodeManager
				, node
				, startIterationIndex >= 0 ? (startIterationIndex+1) : ~startIterationIndex
				, TreeTraverseDirection.Decending);
		}

		/// <summary>
		/// Search for all elements that is less than given key
		/// </summary>
		public IEnumerable<Tuple<K, V>> LessThan (K key)
		{
			var startIterationIndex = 0;

            locker.EnterReadLock();
			var node = FindNodeForIteration (key, this.nodeManager.RootNode, true, ref startIterationIndex);
            locker.ExitReadLock();

			return new TreeTraverser<K, V> (nodeManager
				, node
				, startIterationIndex >= 0 ? startIterationIndex : ~startIterationIndex
				, TreeTraverseDirection.Decending);
		}

		/// <summary>
		/// Very similar to FindNodeForInsertion(), but this handles the case
		/// where the tree has duplicate keys.
		/// </summary>
		/// <param name="moveLeft">In case of duplicate key found, whenever moving cursor to the left or right</param>
		TreeNode<K, V> FindNodeForIteration (K key, TreeNode<K, V> node, bool moveLeft, ref int startIterationIndex)
		{
			// If this node is empty then return it straight away,
			// because it is a non-full root node.
			// Note that we return a bitwise complement of 0, not 0,
			// otherwise caller thinks we found this key at index #0
			if (node.IsEmpty) {
				startIterationIndex = ~0;
				return node;
			}

            locker.EnterReadLock();
			// Perform binary search on specified node
			var binarySearchResult = node.BinarySearchEntriesForKey (key, moveLeft ? true : false);

			// If found, drill down to children node 
			if (binarySearchResult >= 0)
            {
				if (node.IsLeaf)
                {
					// We reached the leaf node, cant drill down any more.
					// Let's start iterating from there
					startIterationIndex = binarySearchResult;
                    locker.ExitReadLock();
					return node;
				}
				else
                {
                    locker.ExitReadLock();
					// What direction to drill down depends on `direction` parameterr
					return FindNodeForIteration (key, node.GetChildNode(moveLeft ? binarySearchResult : binarySearchResult + 1), moveLeft, ref startIterationIndex);
				}
			}
			// Node found, continue searching on the child node which
			// is positiioned at binarySearchResult
			else if (false == node.IsLeaf)
            {
                locker.ExitReadLock();
				return FindNodeForIteration (key, node.GetChildNode(~binarySearchResult), moveLeft, ref startIterationIndex);
			}
			// Otherwise, this is a leaf node, no more children to search,
			// return this one
			else {
				startIterationIndex = binarySearchResult;
                locker.ExitReadLock();
				return node;
			}
		}

		/// <summary>
		/// Search for the node that contains given key, starting from given node
		/// </summary>
		TreeNode<K, V> FindNodeForInsertion (K key, TreeNode<K, V> node, ref int insertionIndex)
		{
			// If this node is empty then return it straight away,
			// because it is a non-full root node.
			// Note that we return a bitwise complement of 0, not 0,
			// otherwise caller thinks we found this key at index #0
			if (node.IsEmpty) {
				insertionIndex = ~0;
				return node;
			}

            locker.EnterReadLock();
			// If X=Vi, for some i, then we are done (X has been found).
			var binarySearchResult = node.BinarySearchEntriesForKey (key);
			if (binarySearchResult >= 0) {
				if (allowDuplicateKeys && false == node.IsLeaf)
                {
                    locker.ExitReadLock();
					return FindNodeForInsertion (key, node.GetChildNode(binarySearchResult), ref insertionIndex);
				} else
                {
					insertionIndex = binarySearchResult;
                    locker.ExitReadLock();
					return node;
				}
			}
			// Otherwise, continue searching on the child node which
			// is positiioned at binarySearchResult
			else if (false == node.IsLeaf)
            {
                locker.ExitReadLock();
				return FindNodeForInsertion (key, node.GetChildNode(~binarySearchResult), ref insertionIndex);
			}
			// Otherwise, this is a leaf node, no more children to search,
			// return this one
			else {
				insertionIndex = binarySearchResult;
                locker.ExitReadLock();
				return node;
			}
		}

		/// <summary>
		/// SEarch for the node that contains given key, starting from the root node
		/// </summary>
		TreeNode<K, V> FindNodeForInsertion (K key, ref int insertionIndex)
		{
			return FindNodeForInsertion (key, nodeManager.RootNode, ref insertionIndex);
		}
	}
}

