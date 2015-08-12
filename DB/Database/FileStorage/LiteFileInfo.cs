using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Netfluid.DB
{
    /// <summary>
    /// Represets a file inside storage collection
    /// </summary>
    public class LiteFileInfo
    {
        /// <summary>
        /// File id have a specific format - it's like file path.
        /// </summary>
        public const string ID_PATTERN = @"^[\w-$@!+%;\.]+(\/[\w-$@!+%;\.]+)*$";

        private static Regex IdPattern = new Regex(ID_PATTERN);

        /// <summary>
        /// Number of bytes on each chunk document to store
        /// </summary>
        public const int CHUNK_SIZE = BsonDocument.MAX_DOCUMENT_SIZE - BasePage.PAGE_AVAILABLE_BYTES; // Chunk size is a page less than a max document size

        public string Id { get; private set; }
        public string Filename { get; set; }
        public string MimeType { get; set; }
        public long Length { get; private set; }
        public DateTime UploadDate { get; internal set; }
        public BsonDocument Metadata { get; set; }

        private LiteDatabase _db;

        public LiteFileInfo(string id)
            : this(id, id)
        {
        }

        public LiteFileInfo(string id, string filename)
        {
            if (!IdPattern.IsMatch(id)) throw LiteException.InvalidFormat("FileId", id);

            Id = id;
            Filename = Path.GetFileName(filename);
            MimeType = MimeTypes.GetType(Filename);
            Length = 0;
            UploadDate = DateTime.Now;
            Metadata = new BsonDocument();
        }

        internal LiteFileInfo(LiteDatabase db, BsonDocument doc)
        {
            _db = db;

            Id = doc["_id"].AsString;
            Filename = doc["filename"].AsString;
            MimeType = doc["mimeType"].AsString;
            Length = doc["length"].AsInt64;
            UploadDate = doc["uploadDate"].AsDateTime;
            Metadata = doc["metadata"].AsDocument;
        }

        public BsonDocument AsDocument
        {
            get
            {
                var doc = new BsonDocument();

                doc["_id"] = Id;
                doc["filename"] = Filename;
                doc["mimeType"] = MimeType;
                doc["length"] = Length;
                doc["uploadDate"] = UploadDate;
                doc["metadata"] = Metadata ?? new BsonDocument();

                return doc;
            }
        }

        internal IEnumerable<BsonDocument> CreateChunks(Stream stream)
        {
            var buffer = new byte[CHUNK_SIZE];
            var read = 0;
            var index = 0;

            while ((read = stream.Read(buffer, 0, LiteFileInfo.CHUNK_SIZE)) > 0)
            {
                Length += (long)read;

                var chunk = new BsonDocument();

                chunk["_id"] = GetChunckId(Id, index++); // index zero based

                if (read != CHUNK_SIZE)
                {
                    var bytes = new byte[read];
                    Array.Copy(buffer, bytes, read);
                    chunk["data"] = bytes;
                }
                else
                {
                    chunk["data"] = buffer;
                }

                yield return chunk;
            }

            yield break;
        }

        /// <summary>
        /// Returns chunck Id for a file
        /// </summary>
        internal static string GetChunckId(string fileId, int index)
        {
            return string.Format("{0}\\{1:00000}", fileId, index);
        }

        /// <summary>
        /// Open file stream to read from database
        /// </summary>
        public LiteFileStream OpenRead()
        {
            if (_db == null) throw LiteException.NoDatabase();

            return new LiteFileStream(_db, this);
        }

        /// <summary>
        /// Save file content to a external file
        /// </summary>
        public void SaveAs(string filename, bool overwritten = true)
        {
            if (_db == null) throw LiteException.NoDatabase();

            using (var file = new FileStream(filename, overwritten ? FileMode.Create : FileMode.CreateNew))
            {
                OpenRead().CopyTo(file);
            }
        }

        /// <summary>
        /// Copy file content to another stream
        /// </summary>
        public void CopyTo(Stream stream)
        {
            using (var reader = OpenRead())
            {
                reader.CopyTo(stream);
            }
        }
    }
}
