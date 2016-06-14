﻿using Netfluid.DB;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netfluid.Dns
{
    public class DnsDatabase
    {
        KeyValueStore<Record> store;
        Tree<string, string> domainIndex;

        public DnsDatabase(string path)
        {
            store = new KeyValueStore<Record>(path);
            domainIndex = Index.MultipleStringIndex(Path.Combine(store.Directory, store.Name + ".domain.sidx"));
        }

        public Record ByID(string id)
        {
            return store.Get(id);
        }

        public IEnumerable<Record> ByDomain(string domain)
        {
            return domainIndex.EqualTo(domain).Select(x=> store.Get(x.Item2));
        }

        public Response GetResponse(Request request)
        {
            var resp = new Response();
            foreach (var question in request)
            {
                var found = ByDomain(question.QName);

                if (found.Any())
                {
                    var qtype = found.Where(x=>x.RecordType == (RecordType)question.QType);

                    if (qtype.Any()) resp.Answers.AddRange(qtype); else resp.Additionals.AddRange(found);
                }
            }
            return resp;
        } 

        public void Insert(Record record)
        {
            store.Insert(record.RecordId, record);
        }

        public void Delete(Record record)
        {
            store.Delete(record.RecordId);
        }

        public void Update(Record record)
        {
            store.Update(record.RecordId,record);
        }
    }
}
