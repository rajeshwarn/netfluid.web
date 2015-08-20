using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netfluid.DB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
        #region Properties + Ctor

        public Settings ConnectionString { get; private set; }

        internal RecoveryService Recovery { get; private set; }

        internal CacheService Cache { get; private set; }

        internal DiskService Disk { get; private set; }

        internal PageService Pager { get; private set; }

        internal JournalService Journal { get; private set; }

        internal TransactionService Transaction { get; private set; }

        internal IndexService Indexer { get; private set; }

        internal DataService Data { get; private set; }

        internal CollectionService Collections { get; private set; }

        public BsonMapper Mapper { get; set; }

        /// <summary>
        /// Starts LiteDB database. Open database file or create a new one if not exits
        /// </summary>
        /// <param name="connectionString">Full filename or connection string</param>
        public LiteDatabase(string connectionString)
        {
            ConnectionString = new Settings(connectionString);

            if (!File.Exists(ConnectionString.Filename))
            {
                DiskService.CreateNewDatafile(ConnectionString);
            }

            Mapper = BsonMapper.Global;

            Recovery = new RecoveryService(ConnectionString);

            Recovery.TryRecovery();

            Disk = new DiskService(ConnectionString);

            Cache = new CacheService(Disk);

            Pager = new PageService(Disk, Cache);

            Journal = new JournalService(ConnectionString, Cache);

            Indexer = new IndexService(Cache, Pager);

            Transaction = new TransactionService(Disk, Cache, Journal);

            Data = new DataService(Disk, Cache, Pager);

            Collections = new CollectionService(Cache, Pager, Indexer, Data);

            UpdateDatabaseVersion();
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
            where T : new()
        {
            return new LiteCollection<T>(this, name);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            return new LiteCollection<BsonDocument>(this, name);
        }

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            Transaction.AvoidDirtyRead();

            return Collections.GetAll().Select(x => x.CollectionName);
        }

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case unsensitive
        /// </summary>
        public bool CollectionExists(string name)
        {
            Transaction.AvoidDirtyRead();

            return Collections.Get(name) != null;
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            return GetCollection(name).Drop();
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            Transaction.Begin();

            try
            {
                var col = Collections.Get(oldName);

                if (col == null || CollectionExists(newName))
                {
                    Transaction.Abort();
                    return false;
                }

                col.CollectionName = newName;
                col.IsDirty = true;

                Transaction.Commit();
            }
            catch
            {
                Transaction.Rollback();
                throw;
            }

            return true;
        }

        #endregion

        #region FileStorage

        private LiteFileStorage _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public LiteFileStorage FileStorage
        {
            get { return _fs ?? (_fs = new LiteFileStorage(this)); }
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Starts a new transaction. After this command, all write operations will be first in memory and will persist on disk
        /// only when call Commit() method. If any error occurs, a Rollback() method will run.
        /// </summary>
        public void BeginTrans()
        {
            Transaction.Begin();
        }

        /// <summary>
        /// Persist all changes on disk. Always use this method to finish your changes on database
        /// </summary>
        public void Commit()
        {
            Transaction.Commit();
        }

        /// <summary>
        /// Cancel all write operations and keep datafile as is before BeginTrans() called.
        /// Rollback are implicit on a database operation error, so you do not need call for database errors (only on business rules).
        /// </summary>
        public void Rollback()
        {
            Transaction.Rollback();
        }

        #endregion

        public void Dispose()
        {
            Disk.Dispose();
            Cache.Dispose();
        }
    }
}
