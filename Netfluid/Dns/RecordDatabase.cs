using Netfluid.DB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Netfluid.Dns
{
    public class RecordDatabase
    {
        readonly Stream mainDatabaseFile;
        readonly Stream primaryIndexFile;

        RecordStorage Storage;
        Tree<string, uint> PrimaryIndex;
        public long Count { get; private set; }

        public RecordDatabase(string path)
        {
            path = Path.GetFullPath(path);
            var name = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path);

            mainDatabaseFile = new FileStream(Path.Combine(dir, name + ".data"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            primaryIndexFile = new FileStream(Path.Combine(dir, name + ".pidx"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            Storage = new RecordStorage(new BlockStorage(mainDatabaseFile, 4096, 48));

            PrimaryIndex = new Tree<string, uint>(new TreeDiskNodeManager<string, uint>(DB.Serializer.String, DB.Serializer.UInt, new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))), true);

            Count = PrimaryIndex.All.Count();
        }
    }
}
