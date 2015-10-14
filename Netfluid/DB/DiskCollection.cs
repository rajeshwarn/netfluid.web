﻿using System;
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

        public long Count { get; private set; }

        public DiskCollection(string path)
        {
            path = Path.GetFullPath(path);
            var name = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path);

            mainDatabaseFile = new FileStream(Path.Combine(dir,name+".data"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            primaryIndexFile = new FileStream(Path.Combine(dir, name + ".pidx"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            Storage = new RecordStorage(new BlockStorage(mainDatabaseFile, 4096, 48));

            PrimaryIndex = new Tree<string, uint>(new TreeDiskNodeManager<string, uint>(new TreeStringSerialzier(), new TreeUIntSerializer(), new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))), false);

            storeLocker = new ReaderWriterLockSlim();
            indexLocker = new ReaderWriterLockSlim();

            Count = PrimaryIndex.LargerThanOrEqualTo("").Count();
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
            Count++;
        }


        public byte[] Pop()
        {
            indexLocker.EnterReadLock();

            var last = PrimaryIndex.LargerThanOrEqualTo("").Select(x => x.Item1).LastOrDefault();

            byte[] found=null;
            if (last != null)
                found = Get(last);

            indexLocker.ExitReadLock();

            if(last!=null)
                Delete(last);

            Count--;
            return found;
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

            Count++;
            return r;
        }

        public byte[] Get(string id)
        {
            byte[] bytes;

            Tuple<string, uint> rd;

            indexLocker.EnterReadLock();
            rd = PrimaryIndex.Get(id);
            indexLocker.ExitReadLock();

            if (rd == null) throw new KeyNotFoundException(id);

            storeLocker.EnterReadLock();
            bytes = Storage.Find(rd.Item2);
            storeLocker.ExitReadLock();

            return DeCompress(bytes);
        }

        public string Last
        {
            get
            {
                indexLocker.EnterReadLock();
                var last = PrimaryIndex.LargerThanOrEqualTo("").LastOrDefault();
                indexLocker.ExitReadLock();

                return last != null ? last.Item1 : null;
            }
        }

        public string First
        {
            get
            {
                indexLocker.EnterReadLock();
                var last = PrimaryIndex.LargerThanOrEqualTo("").FirstOrDefault();
                indexLocker.ExitReadLock();

                return last != null ? last.Item1 : null;
            }
        }

        public void ForEach(Action<string> act)
        {
            indexLocker.EnterReadLock();
            var all = PrimaryIndex.LargerThanOrEqualTo("");

            foreach (var item in all)
            {
                act(item.Item1);
            }

            indexLocker.ExitReadLock();
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

            Count--;
        }
    }
}