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
        KeyValueStore store;
        Tree<string, string> domainIndex;

        public RecordDatabase(string path)
        {
            store = new KeyValueStore(path);
        }
    }
}
