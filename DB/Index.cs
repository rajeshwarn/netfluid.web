using System;
using System.IO;

namespace Netfluid.DB
{
    class Index
    {
        public static Tree<string,string> UniqueStringIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<string, string>(new TreeDiskNodeManager<string, string>(Serializer.String,Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), false);
        }

        public static Tree<string, string> MultipleStringIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<string, string>(new TreeDiskNodeManager<string, string>(Serializer.String, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), true);
        }

        public static Tree<DateTime, string> UniqueDateTimeIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<DateTime, string>(new TreeDiskNodeManager<DateTime, string>(Serializer.DateTime, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), false);
        }

        public static Tree<DateTime, string> MultipleDateTimeIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<DateTime, string>(new TreeDiskNodeManager<DateTime, string>(Serializer.DateTime, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), true);
        }

        public static Tree<long, string> UniqueLongIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<long, string>(new TreeDiskNodeManager<long, string>(Serializer.Long, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), false);
        }

        public static Tree<long, string> MultipleLongIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<long, string>(new TreeDiskNodeManager<long, string>(Serializer.Long, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), true);
        }

        public static Tree<int, string> UniqueIntIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<int, string>(new TreeDiskNodeManager<int, string>(Serializer.Int, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), false);
        }

        public static Tree<int, string> MultipleIntIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<int, string>(new TreeDiskNodeManager<int, string>(Serializer.Int, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), true);
        }

        public static Tree<double, string> UniqueDoubleIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<double, string>(new TreeDiskNodeManager<double, string>(Serializer.Double, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), false);
        }

        public static Tree<double, string> MultipleDoubleIndex(string path)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            return new Tree<double, string>(new TreeDiskNodeManager<double, string>(Serializer.Double, Serializer.String, new RecordStorage(new BlockStorage(stream, 4096))), true);
        }
    }
}
