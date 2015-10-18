using System;
using System.IO;
using System.Text;

namespace Netfluid.DB.Indexes
{
    public class UniqueStringIndex:IDisposable, ISerializer<string>
    {
        FileStream stream;
        Tree<string, string> tree;
        
        public UniqueStringIndex(string path)
        {
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            tree = new Tree<string, string>(new TreeDiskNodeManager<string, string>(this,this, new RecordStorage(new BlockStorage(stream, 4096))), false);
        }

        public void Dispose()
        {
            stream.Flush();
            stream.Dispose();
        }

        #region STRING SERIALIZATION
        public byte[] Serialize(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public string Deserialize(byte[] buffer, int offset, int length)
        {
            return Encoding.UTF8.GetString(buffer, offset, length);
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public int Length
        {
            get
            {
                throw new InvalidOperationException();
            }
        }
        #endregion
    }
}
