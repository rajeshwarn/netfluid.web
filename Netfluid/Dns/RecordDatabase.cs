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

        ReaderWriterLockSlim storeLocker;
        ReaderWriterLockSlim indexLocker;

        public long Count { get; private set; }

        public RecordDatabase(string path)
        {
            path = Path.GetFullPath(path);
            var name = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path);

            mainDatabaseFile = new FileStream(Path.Combine(dir, name + ".data"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            primaryIndexFile = new FileStream(Path.Combine(dir, name + ".pidx"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            Storage = new RecordStorage(new BlockStorage(mainDatabaseFile, 4096, 48));

            PrimaryIndex = new Tree<string, uint>(new TreeDiskNodeManager<string, uint>(new TreeStringSerialzier(), new TreeUIntSerializer(), new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))), true);

            storeLocker = new ReaderWriterLockSlim();
            indexLocker = new ReaderWriterLockSlim();

            Count = PrimaryIndex.LargerThanOrEqualTo("").Count();
        }

        public IEnumerable<string> ByDomain(string domain)
        {
            return PrimaryIndex.EqualTo(domain).Select(x=>x.Item1);
        }

        public void add(string key,uint val)
        {
            PrimaryIndex.Insert(key, val);
        }
    }
}
