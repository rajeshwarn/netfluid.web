﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NetFluid.Collections
{
    public class XMLRepository<T>:IEnumerable<T>
    {
        private readonly string path;
        private readonly List<T> list; 

        public XMLRepository(string filepath)
        {
            path = Path.GetFullPath(filepath);
            list = new List<T>();

            if (File.Exists(path))
                list = XML.Deserialize<List<T>>(File.ReadAllText(path));
        }

        public void Save()
        {
            Task.Factory.StartNew(() =>
            {
                lock (path)
                {
                    File.WriteAllText(path,list.ToXML());
                }
            });
        }

        public void Add(T elem)
        {
            list.Add(elem);
            Save();
        }

        public int Count
        {
            get { return list.Count; }
        }

        public void Add(IEnumerable<T> elem)
        {
            list.AddRange(elem);
            Save();
        }

        public void Remove(Predicate<T> elem)
        {
            list.RemoveAll(elem);
            Save();
        }

        public void Remove(T elem)
        {
            list.Remove(elem);
            Save();
        }

        public T this[int index]
        {
            get { return list[index]; }
            set
            {
                list[index]=value;
                Save();
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
