using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.Db
{
    //FIXME: to be implemented
    internal class Collection<T>
    {
        DiskCollection disk;
        Dictionary<string, Index> indexes;
        public Collection()
        {
            disk = new DiskCollection();
            indexes = new Dictionary<string, Index>();
        }

        public void AddIndex(string name, Type type)
        {
            indexes.Add(name, new Index(type));
        }
    }
}
