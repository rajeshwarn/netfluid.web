using System;
using System.Collections.Generic;

namespace FluidDB
{
    public partial class Collection<T> where T : IDatabaseObject, new()
    {
        /// <summary>
        /// Insert a new document to this collection. Document Id must be a new value in collection
        /// </summary>
        public virtual void Insert(T doc)
        {
            if (doc == null)
                doc = new T();

            // gets document Id
            if (doc.Id == null)
            {
                //FIXME: Add a better id generation
                doc.Id = Guid.NewGuid().ToString();
            }

                // serialize object
            var bytes = BsonSerializer.Serialize(doc);

            _engine.Transaction.Begin();

            try
            {
                var col = this.GetCollectionPage(true);

                // storage in data pages - returns dataBlock address
                var dataBlock = _engine.Data.Insert(col, new IndexKey(doc.Id), bytes);

                // store id in a PK index [0 array]
                var pk = _engine.Indexer.AddNode(col.PK, doc.Id);

                // do links between index <-> data block
                pk.DataBlock = dataBlock.Position;
                dataBlock.IndexRef[0] = pk.Position;

                // for each index, insert new IndexNode
                for (byte i = 1; i < col.Indexes.Length; i++)
                {
                    var index = col.Indexes[i];

                    if (!index.IsEmpty)
                    {
                        var key = BsonSerializer.GetFieldValue(doc, index.Field);

                        var node = _engine.Indexer.AddNode(index, key);

                        // point my index to data object
                        node.DataBlock = dataBlock.Position;

                        // point my dataBlock
                        dataBlock.IndexRef[i] = node.Position;
                    }
                }

                _engine.Transaction.Commit();
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection
        /// </summary>
        public virtual void Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            try
            {
                _engine.Transaction.Begin();

                foreach (var doc in docs)
                {
                    this.Insert(doc);
                }

                _engine.Transaction.Commit();
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }
        }
    }
}
