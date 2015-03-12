namespace FluidDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams.
    /// </summary>
    public partial class FileStorage
    {
        private Collection<BsonDocument> _files;
        private Collection<BsonDocument> _chunks;
        private FluidDatabase _engine;

        internal FileStorage(FluidDatabase engine)
        {
            _engine = engine;
            _files = _engine.GetCollection("_files");
            _chunks = _engine.GetCollection("_chunks");
        }
    }
}
