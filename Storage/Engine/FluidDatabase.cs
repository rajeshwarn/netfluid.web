using System;
using System.IO;
using System.Linq;

namespace FluidDB
{
    /// <summary>
    /// The LiteDB engine. Used for create a LiteDB instance and use all storage resoures. It's the database connection engine.
    /// </summary>
    public partial class FluidDatabase : IDisposable
    {
        #region Properties + Ctor

        public DbSettings Settings { get; private set; }

        internal RecoveryService Recovery { get; private set; }

        internal CacheService Cache { get; private set; }

        internal DiskService Disk { get; private set; }

        internal PageService Pager { get; private set; }

        internal JournalService Journal { get; private set; }

        internal TransactionService Transaction { get; private set; }

        internal IndexService Indexer { get; private set; }

        internal DataService Data { get; private set; }

        internal CollectionService Collections { get; private set; }

        /// <summary>
        /// Starts LiteDB engine. Open database file or create a new one if not exits
        /// </summary>
        /// <param name="path">Full filename or connection string</param>
        public FluidDatabase(string path)
        {
            Settings = new DbSettings(path);

            if (!File.Exists(Settings.Filename))
            {
                CreateNewDatabase(Settings);
            }

            Recovery = new RecoveryService(Settings);

            Recovery.TryRecovery();

            Disk = new DiskService(Settings);

            Cache = new CacheService(Disk);

            Pager = new PageService(Disk, Cache);

            Journal = new JournalService(Settings, Cache);

            Indexer = new IndexService(Cache, Pager);

            Transaction = new TransactionService(Disk, Cache, Journal);

            Data = new DataService(Disk, Cache, Pager);

            Collections = new CollectionService(Pager, Indexer);

            UpdateDatabaseVersion();
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a strong typed POCO class. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public Collection<T> GetCollection<T>(string name) where T : IDatabaseObject, new()
        {
            return new Collection<T>(this, name);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public Collection<BsonDocument> GetCollection(string name)
        {
            return new Collection<BsonDocument>(this, name);
        }

        #endregion

        #region UserVersion

        /// <summary>
        /// Update database version, when necessary
        /// </summary>
        private void UpdateDatabaseVersion()
        {
            // not necessary "AvoidDirtyRead" because its calls from ctor
            var current = Cache.Header.UserVersion;
            var recent = Settings.UserVersion;

            // there is no updates
            if (current == recent) return;

            // start a transaction
            Transaction.Begin();

            try
            {
                for (var newVersion = current + 1; newVersion <= recent; newVersion++)
                {
                    Cache.Header.UserVersion = newVersion;
                }

                Cache.Header.IsDirty = true;
                Transaction.Commit();

            }
            catch (Exception ex)
            {
                Transaction.Rollback();
                throw ex;
            }
        }

        #endregion

        #region Files Storage

        private FileStorage _files = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public FileStorage FileStorage
        {
            get { return _files ?? (_files = new FileStorage(this)); }
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

        #region Statics methods

        /// <summary>
        /// Create a empty database ready to be used using connectionString as parameters
        /// </summary>
        private static void CreateNewDatabase(DbSettings connectionString)
        {
            using (var stream = File.Create(connectionString.Filename))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // creating header + master collection
                    DiskService.WritePage(writer, new HeaderPage { PageID = 0, LastPageID = 1 });
                    DiskService.WritePage(writer, new CollectionPage { PageID = 1, CollectionName = "_master" });
                }
            }
        }

        #endregion

        public void Dispose()
        {
            Disk.Dispose();
        }
    }
}
