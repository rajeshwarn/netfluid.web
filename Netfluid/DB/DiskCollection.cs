using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Netfluid.DB
{
    class DiskCollection
    {
        readonly Stream mainDatabaseFile;
        readonly Stream primaryIndexFile;

        RecordStorage Storage;
        Tree<string, uint> PrimaryIndex;

        ReaderWriterLockSlim storeLocker;
        ReaderWriterLockSlim indexLocker;

        public DiskCollection(string path)
        {
            var name = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path);

            mainDatabaseFile = new FileStream(Path.Combine(dir,name+".data"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            primaryIndexFile = new FileStream(Path.Combine(dir, name + ".pidx"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            Storage = new RecordStorage(new BlockStorage(mainDatabaseFile, 4096, 48));

            PrimaryIndex = new Tree<string, uint>(new TreeDiskNodeManager<string, uint>(new TreeStringSerialzier(), new TreeUIntSerializer(), new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))), false);

            storeLocker = new ReaderWriterLockSlim();
            indexLocker = new ReaderWriterLockSlim();
        }

        private static byte[] Compress(byte[] bytes)
        {
            var input = new MemoryStream(bytes);
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                input.CopyTo(compressor);
                compressor.Close();
                return compressStream.ToArray();
            }
        }

        private static byte[] DeCompress(byte[] bytes)
        {
            var input = new MemoryStream(bytes);
            using (var output = new MemoryStream())
            using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
            {
                decompressor.CopyTo(output);
                return output.ToArray();
            }
        }

        public void Insert(string id,byte[] obj)
        {
            var bytes = Compress(obj);
            uint r;

            storeLocker.EnterWriteLock();
            r = Storage.Create(bytes);
            storeLocker.ExitWriteLock();

            indexLocker.EnterWriteLock();
            PrimaryIndex.Insert(id, r);
            indexLocker.ExitWriteLock();
        }

        public string Push(byte[] obj)
        {
            var bytes = Compress(obj); 
            uint id;
            string r;

            storeLocker.EnterWriteLock();
            id = Storage.Create(bytes);
            storeLocker.ExitWriteLock();

            r = id.ToString();

            indexLocker.EnterWriteLock();
            PrimaryIndex.Insert(r, id);
            indexLocker.ExitWriteLock();
            return r;
        }

        public byte[] Get(string id)
        {
            byte[] bytes;

            Tuple<string, uint> rd;

            indexLocker.EnterReadLock();
            rd = PrimaryIndex.Get(id);
            indexLocker.ExitReadLock();


            storeLocker.EnterReadLock();
            bytes = Storage.Find(rd.Item2);
            storeLocker.ExitReadLock();

            return DeCompress(bytes);
        }

        public IEnumerable<string> All()
        {
            return new ThreadSafeEnumerator<string>(PrimaryIndex.LargerThanOrEqualTo("").Select(x => x.Item1), indexLocker);
        }

        public void Replace(string id, byte[] obj)
        {
            var bytes = Compress(obj);

            storeLocker.EnterWriteLock();
            Storage.Update(PrimaryIndex.Get(id).Item2, bytes);
            storeLocker.ExitWriteLock();
        }

        public void Delete(string id)
        {
            storeLocker.EnterWriteLock();
            Storage.Delete(PrimaryIndex.Get(id).Item2);
            storeLocker.ExitWriteLock();

            indexLocker.EnterWriteLock();
            PrimaryIndex.Delete(id);
            indexLocker.ExitWriteLock();
        }
    }
}
