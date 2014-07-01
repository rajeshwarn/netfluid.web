using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace NetFluid.Collections
{
    /// <summary>
    /// Work in progress
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XMLRepository<T>:IRepository<T> where T : IDatabaseObject
    {
        private readonly string path;
        private readonly List<T> list;

        public XMLRepository(string path)
        {
            this.path = Path.GetFullPath(path);
            list = new List<T>();

            if (File.Exists(path))
            {
                list = list.FromXML(File.ReadAllText(path));
            }
        }

        public void Remove(T obj)
        {
            lock (this)
            {
                list.Remove(obj);
                File.WriteAllText(path, list.ToXML());
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public IQueryable<T> Queryable
        {
            get { return list.AsQueryable(); }
        }

        public Expression Expression { get { return list.AsQueryable().Expression; } }
        public Type ElementType { get { return list.AsQueryable().ElementType; } }
        public IQueryProvider Provider { get { return list.AsQueryable().Provider; } }

        public T this[string id]
        {
            get { return list.FirstOrDefault(x => x.Id == id); }
        }

        public IEnumerable<T> OfType(Type type)
        {
            return list.Where(x => x.GetType() == type);
        }

        public IEnumerable<T> OfType(string type)
        {
            return OfType(Type.GetType(type));
        }

        public void Save(IEnumerable<T> obj)
        {
            lock (this)
            {
                foreach (var o in obj)
                {
                    if (o.Id == null)
                    {
                        T k = o;
                        k.Id = Security.UID();
                        list.Add(k);
                    }
                    else
                    {
                        list.RemoveAll(x => x.Id == o.Id);
                        list.Add(o);
                    }
                }
                File.WriteAllText(path, list.ToXML());
            }
        }

        public void Save(T obj)
        {
            lock (this)
            {
                if (obj.Id == null)
                    obj.Id = Security.UID();
                else
                    list.RemoveAll(x => x.Id == obj.Id);

                list.Add(obj);
                File.WriteAllText(path, list.ToXML());
            }
        }

        public void Remove(string id)
        {
            lock (this)
            {
                list.RemoveAll(x=>x.Id==id);
                File.WriteAllText(path, list.ToXML());
            }
        }
    }
}
