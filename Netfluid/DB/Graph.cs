using System.Collections.Generic;
using System.Linq;

namespace Netfluid.DB
{
    public class Graph<T>:KeyValueStore<T>
    {
        Tree<string, string> connections;

        public Graph(string path):base(path)
        {
            connections = Index.MultipleStringIndex(System.IO.Path.Combine(Directory, Name + "_connections.sidx"));
        }

        public void AddConnection(string from, string to)
        {
            connections.Insert(from, to);
        }

        public IEnumerable<string> GetConnections(string from)
        {
            return connections.EqualTo(from).Select(X => X.Item2);
        }

        public void RemoveConnection(string from,string to)
        {
            connections.Delete(from, to);
        }

        public override void Delete(string key)
        {
            GetConnections(key).ForEach(x => RemoveConnection(key,x));
            base.Delete(key);
        }
    }
}
