using System;

namespace Netfluid.DB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods. Useful for load reference documents when nedded.
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException("action");

            var col = new LiteCollection<T>(this.Database, Name);

            col._pageID = _pageID;
            col._includes.AddRange(_includes);

            col._includes.Add(action);

            return col;
        }
    }
}
