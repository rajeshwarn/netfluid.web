using System;
using System.Collections.Generic;
using System.Linq;

namespace NetFluid.Collections
{
    [Serializable]
    public class NDimensionalDictionary<T>
    {
        [Serializable]
        class Node<K>
        {
            public int Level;
            public Dictionary<object, Node<K>> childs;
            public Dictionary<object, K> values;

            public long Count
            {
                get
                {
                    long count = values!=null ? values.Count : 0;
                    if (childs != null)
                        count += childs.Sum(x=>x.Value.Count);
                    return count;
                }
            }

            public Dictionary<object, Node<K>> Childs
            {
                get
                {
                    if (childs == null)
                        childs = new Dictionary<object, Node<K>>();
                    return childs;
                }
            }

            public Dictionary<object, K> Values
            {
                get
                {
                    if (values == null)
                        values = new Dictionary<object, K>();
                    return values;
                }
            }
        }

        Node<T> root;

        public void Set(T value, object[] coordinates)
        {
            if (root == null)
                root = new Node<T>() { Level =0 };

            var current = root;
            var length = coordinates.Length - 1;
            for (int i = 0; i < length; i++)
            {
                Node<T> child;
                lock (current)
                {
                    if (!current.Childs.TryGetValue(coordinates[i], out child))
                    {
                        current.Childs.Add(coordinates[i], child = new Node<T>() { Level = current.Level + 1 });
                    }
                }
                current = child;
            }

            lock (current)
            {
                current.Values[coordinates.Last()] = value;
            }
        }

        public T Get(params object[] coordinates)
        {
            if (root == null)
                throw new ArgumentException();

            var current = root;
            var length = coordinates.Length - 1;
            for (int i = 0; i < length; i++)
            {
                if (current.Childs == null)
                    throw new ArgumentException();

                Node<T> child;
                if (!current.Childs.TryGetValue(coordinates[i], out child))
                    throw new ArgumentException();
                current = child;
            }

            if (current.Values == null)
                throw new ArgumentException();

            return current.Values[coordinates.Last()];
        }

        public T this[params object[] coordinates]
        {
            get { return Get(coordinates); }
            set { Set(value, coordinates); }
        }

        public long Count
        {
            get { return root.Count; }
        }
    }
}
