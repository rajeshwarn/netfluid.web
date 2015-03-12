using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;

namespace FluidDB
{
    /// <summary>
    /// This class contains only static method for serialize/deserialize objects
    /// and Get/Set informations on poco objects or BsonDocument
    /// </summary>
    internal class BsonSerializer
    {
        public static byte[] Serialize(object obj)
        {
            var ms = new MemoryStream();
            var serializer = new JsonSerializer();
            var writer = new BsonWriter(ms);

            serializer.Serialize(writer, obj is BsonDocument ? (obj as BsonDocument).RawValue : obj);

            return ms.ToArray();
        }

        public static T Deserialize<T>(IndexKey key, byte[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentNullException("data");

            IDatabaseObject doc;
            var ms = new MemoryStream(data);
            var serializer = new JsonSerializer();
            var reader = new BsonReader(ms);

            if (typeof(T) == typeof(BsonDocument))
            {
                var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
                doc = new BsonDocument(dict);
            }
            else
            {
                doc = serializer.Deserialize<T>(reader) as IDatabaseObject;
            }

            doc.Id = key.Value as string;

            return (T)doc;
        }

        /// <summary>
        /// Gets from a document object (plain C# object or BsonDocument) some field value
        /// </summary>
        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj is BsonDocument)
            {
                var doc = (BsonDocument)obj;

                return doc[fieldName].RawValue;
            }
            else
            {
                var p = obj.GetType().GetProperty(fieldName);

                return p == null ? null : p.GetValue(obj, null);
            }
        }
    }
}
