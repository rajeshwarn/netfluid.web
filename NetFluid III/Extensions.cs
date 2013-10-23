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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NetFluid.Serialization;
using NetFluid.HTTP;

namespace NetFluid
{
    public static class Extensions
    {
        #region STRING

        public static bool EndsWith(this string str, char c)
        {
            return str[str.Length - 1] == c;
        }

        public static string HTMLEncode(this string str)
        {
            return HttpUtility.HtmlEncode(str);
        }

        public static string HTMLDecode(this string str)
        {
            return HttpUtility.HtmlDecode(str);
        }

        public static string StripHTML(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            var array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;
            bool spacebefore = false;

            foreach (char @let in source)
            {
                if (@let == '<')
                {
                    inside = true;
                    continue;
                }
                if (@let == '>')
                {
                    inside = false;
                    continue;
                }
                if (inside)
                    continue;

                if (char.IsWhiteSpace(@let))
                {
                    if (!spacebefore && arrayIndex != 0 && !char.IsWhiteSpace(array[arrayIndex]))
                    {
                        array[arrayIndex] = ' ';
                        arrayIndex++;
                    }
                    spacebefore = true;
                }
                else
                {
                    array[arrayIndex] = @let;
                    arrayIndex++;
                    spacebefore = false;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        #endregion

        #region ENUMERABLE

        public static T Random<T>(this IEnumerable<T> source)
        {
            int c = source.Count();
            var r = new Random();
            return source.Skip(r.Next(c - 1)).First();
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using (IEnumerator<T> enumerator = source.GetEnumerator())
                while (enumerator.MoveNext())
                    yield return YieldBatchElements(enumerator, batchSize - 1);
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
                yield return source.Current;
        }

        public static void ForEach<T>(this IEnumerable<T> enu, Action<T> act)
        {
            foreach (T obj in enu)
            {
                act.Invoke(obj);
            }
        }
        
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enu, T elem)
        {
        	return enu.Concat(new[]{elem});
        }

        #endregion

        #region DATETIME

        public static double ToUnixTimestamp(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Math.Round((date - epoch).TotalSeconds);
        }

        #endregion

        #region SERIALIZATION

        public static T FromBinary<T>(this T obj, byte[] bytes)
        {
            return Binary.Deserialize<T>(bytes);
        }

        public static byte[] ToBinary(this object obj)
        {
            return Binary.Serialize(obj);
        }

        public static T FromXML<T>(this T obj, string xml)
        {
            return XML.Deserialize<T>(xml);
        }

        public static string ToXML(this object obj)
        {
            return XML.Serialize(obj);
        }

        public static T FromJSON<T>(this T obj, string yaml)
        {
            return JSON.Deserialize<T>(yaml);
        }

        public static string ToJSON(this object obj)
        {
            return JSON.Serialize(obj);
        }

        #endregion

        #region METHODINFO

        public static bool HasAttribute<T>(this MethodInfo type) where T : Attribute
        {
            bool b = type.GetCustomAttributes(false).OfType<T>().Any();
            return b;
        }

        public static T[] CustomAttribute<T>(this MethodInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(true).OfType<T>().ToArray();
        }

        #endregion

        #region FIELD INFO

        public static bool HasAttribute<T>(this FieldInfo type) where T : Attribute
        {
            bool b = type.GetCustomAttributes(false).OfType<T>().Any();
            return b;
        }

        public static T[] CustomAttribute<T>(this FieldInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(true).OfType<T>().ToArray();
        }

        #endregion

        #region PROPERTYINFO

        public static object GetValue(this PropertyInfo pi, object obj)
        {
            return pi.GetGetMethod(true).Invoke(obj, null);
        }

        public static bool HasAttribute<T>(this PropertyInfo type) where T : Attribute
        {
            bool b = type.GetCustomAttributes(false).OfType<T>().Any();
            return b;
        }

        public static T[] CustomAttribute<T>(this PropertyInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(true).OfType<T>().ToArray();
        }

        #endregion

        #region TYPE

        public static object DefaultValue(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool HasAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            bool b = type.GetCustomAttributes(inherit).OfType<T>().Any();
            return b;
        }

        public static T[] CustomAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().ToArray();
        }

        public static Type[] Ancestor(this Type me)
        {
            Type t = me;
            var result = new List<Type>();
            while (t != typeof (object) && t != null)
            {
                if (t != typeof (object))
                {
                    result.Add(t);
                }
                t = t.BaseType;
            }
            return result.ToArray();
        }

        public static bool Inherit(this Type me, Type type)
        {
            Type t = me;

            while (t != typeof (object) && t != null)
            {
                if (t == type)
                {
                    return true;
                }
                t = t.BaseType;
            }
            return false;
        }

        public static bool Implements(this Type type, Type @interface)
        {
            return type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == @interface);
        }

        public static object CreateIstance(this Type type, params object[] obj)
        {
            return (obj == null || obj.Length == 0)
                       ? Activator.CreateInstance(type)
                       : Activator.CreateInstance(type, obj);
        }

        #endregion

        #region STREAM

        public static byte[] BinaryRead(this Stream s)
        {
            var b = new byte[s.Length - s.Position];
            s.Read(b, 0, b.Length);
            return b;
        }

        #endregion
    }
}