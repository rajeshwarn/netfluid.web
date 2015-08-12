using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Netfluid.Collections
{
    /// <summary>
    /// Generic interface for ORMs collections
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepository<T> : IQueryable<T>
    {
        /// <summary>
        /// Return an iterator to all item into the collection
        /// </summary>
        IQueryable<T> Queryable { get; }

        /// <summary>
        /// Return the item with this id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T this[string id] { get; }

        /// <summary>
        /// All items of given type
        /// </summary>
        /// <param name="type">The type you want to select</param>
        /// <returnsAn iterator to all items in the collection wich have that type<returns>
        IEnumerable<T> OfType(Type type);
        /// <summary>
        /// All items of given type
        /// </summary>
        /// <param name="type">The type you want to select</param>
        /// <returnsAn iterator to all items in the collection wich have that type<returns>
        IEnumerable<T> OfType(string type);

        /// <summary>
        /// Bulk collection insert or update of elements
        /// </summary>
        /// <param name="obj">collection of items to be saved or updated</param>
        void Save(IEnumerable<T> obj);

        /// <summary>
        /// Single item insert or update
        /// </summary>
        /// <param name="obj"></param>
        void Save(T obj);

        /// <summary>
        /// Remove an item from the database collection
        /// </summary>
        /// <param name="id"></param>
        void Remove(string id);

        /// <summary>
        /// Remove an item from the collection
        /// </summary>
        /// <param name="obj"></param>
        void Remove(T obj);
    }
}