using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Netfluid.DB.Indexes
{
    public class UniqueStringIndex:IDisposable, ISerializer<string>,IIndex<string,string>
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

        public bool Delete(string key)
        {
            throw new NotImplementedException();
        }

        public bool Delete(string key, string value, IComparer<string> valueComparer = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<string, string>> EqualTo(string key)
        {
            throw new NotImplementedException();
        }

        public Tuple<string, string> Get(string key)
        {
            throw new NotImplementedException();
        }

        public void Insert(string key, string value)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<string, string>> LargerThan(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<string, string>> LargerThanOrEqualTo(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<string, string>> LessThan(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<string, string>> LessThanOrEqualTo(string key)
        {
            throw new NotImplementedException();
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

        public IEnumerable<Tuple<string, string>> All
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
