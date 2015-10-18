using Netfluid.DB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netfluid.Dns
{
    public class RecordDatabase
    {
        KeyValueStore store;
        Tree<string, string> domainIndex;

        public RecordDatabase(string path)
        {
            store = new KeyValueStore(path);
            domainIndex = Index.MultipleStringIndex(Path.Combine(store.Directory, store.Name + ".domain.sidx"));
        }

        public IEnumerable<Record> ByDomain(string domain)
        {
            return domainIndex.EqualTo(domain).Select(x=> store.Get<Record>(x.Item2));
        }

        public void Insert(Record record)
        {
            if (string.IsNullOrEmpty(record.RecordId))
                record.RecordId = store.Push(record.RecordId)+Security.UID();
            else
                store.Insert(record.RecordId, record);
        }
    }
}
