using Netfluid.Json.Bson;
using System;
using System.IO;

namespace Netfluid
{
    public static class BSON
    {
        public static void Serialize(object obj, BinaryWriter writer)
        {
            var w = new BsonWriter(writer);
            var s = new Json.JsonSerializer();
            s.Serialize(w, obj);
        }

        public static void Serialize(object obj, Stream stream)
        {
            var w = new BsonWriter(stream);
            var s = new Json.JsonSerializer();
            s.Serialize(w, obj);
        }

        public static byte[] Serialize(object obj)
        {
            var m = new MemoryStream();
            var w = new BsonWriter(m);
            var s = new Json.JsonSerializer();
            s.Serialize(w, obj);
            return m.ToArray();
        }

        public static object Deserialize(BinaryReader reader)
        {
            var r = new BsonReader(reader);
            var s = new Json.JsonSerializer();
            return s.Deserialize(r);
        }

        public static T Deserialize<T>(BinaryReader reader)
        {
            var r = new BsonReader(reader);
            var s = new Json.JsonSerializer();
            return (T)s.Deserialize(r,typeof(T));
        }

        public static object Deserialize(BinaryReader reader,Type t)
        {
            var r = new BsonReader(reader);
            var s = new Json.JsonSerializer();
            return s.Deserialize(r,t);
        }

        public static object Deserialize(Stream stream)
        {
            var r = new BsonReader(stream);
            var s = new Json.JsonSerializer();
            return s.Deserialize(r);
        }

        public static T Deserialize<T>(Stream stream)
        {
            var r = new BsonReader(stream);
            var s = new Json.JsonSerializer();
            return (T)s.Deserialize(r,typeof(T));
        }

        public static object Deserialize(Stream stream,Type t)
        {
            var r = new BsonReader(stream);
            var s = new Json.JsonSerializer();
            return s.Deserialize(r,t);
        }

        public static object Deserialize(byte[] bytes)
        {
            var r = new BsonReader(new MemoryStream(bytes));
            var s = new Json.JsonSerializer();
            return s.Deserialize(r);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            var r = new BsonReader(new MemoryStream(bytes));
            var s = new Json.JsonSerializer();
            return (T)s.Deserialize(r,typeof(T));
        }

        public static object Deserialize(byte[] bytes,Type t)
        {
            var r = new BsonReader(new MemoryStream(bytes));
            var s = new Json.JsonSerializer();
            return s.Deserialize(r,t);
        }
    }
}
