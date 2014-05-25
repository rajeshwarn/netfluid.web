// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetFluid
{
    /// <summary>
    /// .NET binary serializer/deserializer
    /// </summary>
    public static class Binary
    {
        /// <summary>
        /// Serialize the object to the stream
        /// </summary>
        /// <param name="obj">object to serialize</param>
        /// <param name="s">target stream</param>
        public static void Serialize(object obj, Stream s)
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(s, obj);
        }

        /// <summary>
        /// Deserialize an object form the stream
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="s">reading stream</param>
        /// <returns>deserialized T object</returns>
        public static T Deserialize<T>(Stream s)
        {
            var formatter = new BinaryFormatter();
            object dbg = formatter.Deserialize(s);
            return (T) dbg;
        }

        /// <summary>
        /// Deserialize an object form the bynary array
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="b">binary serialized value</param>
        /// <returns>deserialized T object</returns>
        public static T Deserialize<T>(byte[] b)
        {
            var formatter = new BinaryFormatter();
            var s = new MemoryStream();
            s.Write(b, 0, b.Length);
            s.Seek(0, SeekOrigin.Begin);
            object dbg = formatter.Deserialize(s);
            return (T) dbg;
        }

        /// <summary>
        /// Binary serialize an object
        /// </summary>
        /// <param name="obj">object to be serialized</param>
        /// <returns></returns>
        public static byte[] Serialize(object obj)
        {
            var formatter = new BinaryFormatter();
            var s = new MemoryStream();
            formatter.Serialize(s, obj);
            return s.ToArray();
        }


        /// <summary>
        /// Deserialize a binary serialized object
        /// </summary>
        /// <param name="b">binary serialized value</param>
        /// <param name="type">object target type</param>
        /// <returns>deserialized object</returns>
        public static object Deserialize(byte[] b, Type type)
        {
            var formatter = new BinaryFormatter();
            var s = new MemoryStream();
            s.Write(b, 0, b.Length);
            s.Seek(0, SeekOrigin.Begin);
            object dbg = formatter.Deserialize(s);
            return dbg;
        }
    }
}