using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Netfluid.DB
{
    public class BinaryPersistentList
    {
        Stream stream;
        RecordStorage storage;

        public BinaryPersistentList(string path)
        {
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            storage = new RecordStorage(new BlockStorage(stream, 4096, 48));

            for (Count = 0; Count < uint.MaxValue; Count++)
            {
                if (this[Count] == null) break;
            }
        }

        private static byte[] Compress(byte[] bytes)
        {
            if (bytes == null) return null;

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
            if (bytes == null) return null;

            var input = new MemoryStream(bytes);
            using (var output = new MemoryStream())
            using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
            {
                decompressor.CopyTo(output);
                return output.ToArray();
            }
        }

        public uint Count { get; private set; }

        public void Add(byte[] b)
        {
            storage.Create(Compress(b));
            Count++;
        }

        public byte[] this[uint index]
        {
            get
            {
                return DeCompress(storage.Find(index));
            }
        }
    }
}
