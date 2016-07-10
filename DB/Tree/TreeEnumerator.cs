using System;
using System.Collections.Generic;
using System.Collections;

namespace Netfluid.DB
{
	internal class TreeEnumerator<K, V> : IEnumerator<Tuple<K, V>>
	{
		readonly TreeDiskNodeManager<K, V> nodeManager;
		readonly TreeTraverseDirection direction;

		bool doneIterating = false;

		public TreeNode<K, V> CurrentNode { get; private set; }

		public int CurrentEntry { get; private set; }

        object IEnumerator.Current {
			get {
				return Current;
			}
		}

		public Tuple<K, V> Current { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sdb.BTree.TreeEnumerator`2"/> class.
        /// </summary>
        /// <param name="nodeManager">Node manager.</param>
        /// <param name="node">Node.</param>
        /// <param name="fromIndex">From index.</param>
        /// <param name="direction">Direction.</param>
        public TreeEnumerator (TreeDiskNodeManager<K, V> nodeManager
			, TreeNode<K, V> node
			, int fromIndex
			, TreeTraverseDirection direction)
		{
			this.nodeManager = nodeManager;
			this.CurrentNode = node;
			this.CurrentEntry = fromIndex;
			this.direction = direction;
		}

		public bool MoveNext ()
		{
			if (doneIterating) {
				return false;
			}

			switch (this.direction) {
				case TreeTraverseDirection.Ascending:
					return MoveForward ();
				case TreeTraverseDirection.Decending:
					return MoveBackward ();
				default:
					throw new ArgumentOutOfRangeException ();
			}
		}

		bool MoveForward ()
		{
			// Leaf node, either move right or up
			if (CurrentNode.IsLeaf)
			{
				// First, move right
				CurrentEntry++;

				while (true)
				{
					// If currentEntry is valid
					// then we are done here.
					if (CurrentEntry < CurrentNode.EntriesCount) {
						Current = CurrentNode.GetEntry (CurrentEntry);
						return true;
					}
					// If can't move right then move up
					else if (CurrentNode.ParentId != 0){
						CurrentEntry = CurrentNode.IndexInParent ();
						CurrentNode = nodeManager.Find (CurrentNode.ParentId);

						// Validate move up result
						if ((CurrentEntry < 0) || (CurrentNode == null)) {
							throw new Exception ("Something gone wrong with the BTree");
						}
					}
					// If can't move up when we are done iterating
					else {
						Current = null;
						doneIterating = true;
						return false;
					}
				}
			}
			// Parent node, always move right down
			else {
				CurrentEntry++; // Increase currentEntry, this make firstCall to nodeManager.Find 
				                // to return the right node, but does not affect subsequence calls

				do {
					CurrentNode = CurrentNode.GetChildNode(CurrentEntry);
					CurrentEntry = 0;
				} while (false == CurrentNode.IsLeaf);

				Current = CurrentNode.GetEntry (CurrentEntry);
				return true;
			}
		}

		bool MoveBackward ()
		{
			// Leaf node, either move right or up
			if (CurrentNode.IsLeaf)
			{
				// First, move left
				CurrentEntry--;

				while (true)
				{
					// If currentEntry is valid
					// then we are done here.
					if (CurrentEntry >= 0) {
						Current = CurrentNode.GetEntry (CurrentEntry);
						return true;
					}
					// If can't move left then move up
					else if (CurrentNode.ParentId != 0){
						CurrentEntry = CurrentNode.IndexInParent () -1;
						CurrentNode = nodeManager.Find (CurrentNode.ParentId);

						// Validate move result
						if (CurrentNode == null) {
							throw new Exception ("Something gone wrong with the BTree");
						}
					}
					// If can't move up when we are done here
					else {
						doneIterating = true;
						Current = null;
						return false;
					}
				}
			}
			// Parent node, always move left down
			else {
				do {
					CurrentNode = CurrentNode.GetChildNode(CurrentEntry);
					CurrentEntry = CurrentNode.EntriesCount;

					// Validate move result
					if ((CurrentEntry < 0) || (CurrentNode == null)) {
						throw new Exception ("Something gone wrong with the BTree");
					}
				} while (false == CurrentNode.IsLeaf);

				CurrentEntry -= 1;
				Current = CurrentNode.GetEntry (CurrentEntry);
				return true;
			}
		}

		public void Reset ()
		{
			throw new NotSupportedException ();
		}

		public void Dispose ()
		{
			// dispose my ass
		}
	}
}

