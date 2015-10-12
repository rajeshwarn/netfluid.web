using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.DB
{
    //FIXME:to be implemented
    class Index
    {
        GenericSerializer serializer;
        FileStream indexFile;
        Tree<object, uint> tree;

        public Index(Type type)
        {
            indexFile = new FileStream("test.sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            tree = new Tree<object, uint>(new TreeDiskNodeManager<object, uint>(serializer, new TreeUIntSerializer(), new RecordStorage(new BlockStorage(indexFile, 4096))), false);

            serializer = new GenericSerializer(type);
        }
    }
}
