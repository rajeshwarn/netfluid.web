using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.DB
{
    public class StringGraph
    {
        Tree<string, string> index;

        public StringGraph(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            index = new Tree<string, string>(new TreeDiskNodeManager<string, string>(Serializer.String, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), true);
        }

        public void Insert(string from, string to)
        {
            if (!index.EqualTo(from).Where(x=>x.Item2==to).Any())
                index.Insert(from, to);
        }
    }
}
