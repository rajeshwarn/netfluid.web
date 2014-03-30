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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using NetFluid.HTTP;
using System.Collections.Concurrent;

namespace NetFluid
{
    public static class Extensions
    {
        #region EXCEPTION

        public static string ToHTML(this Exception ex)
        {
            var sb = new StringBuilder("<h1>Exception "+ex.GetType().Name+"</h1>");
            sb.Append("<h2>" + ex.Message + "<h2>");
            sb.Append("<h2>StackTrace</h2>");
            sb.Append("<div>" + ex.StackTrace + "</div>");
            if (ex.InnerException!=null)
            {
                sb.Append("<h2>Inner exception</h2>");
                sb.Append(ex.InnerException.ToHTML());
            }
            return sb.ToString();
        }
        #endregion

        #region IP ADDRESS
        private static readonly IPAddress _ipv4MulticastNetworkAddress = IPAddress.Parse("224.0.0.0");
        private static readonly IPAddress _ipv6MulticastNetworkAddress = IPAddress.Parse("FF00::");

        /// <summary>
        ///     Reverses the order of the bytes of an IPAddress
        /// </summary>
        /// <param name="ipAddress"> Instance of the IPAddress, that should be reversed </param>
        /// <returns> New instance of IPAddress with reversed address </returns>
        public static IPAddress Reverse(this IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            byte[] addressBytes = ipAddress.GetAddressBytes();
            var res = new byte[addressBytes.Length];

            for (int i = 0; i < res.Length; i++)
            {
                res[i] = addressBytes[addressBytes.Length - i - 1];
            }

            return new IPAddress(res);
        }

        /// <summary>
        ///     Gets the network address for a specified IPAddress and netmask
        /// </summary>
        /// <param name="ipAddress"> IPAddress, for that the network address should be returned </param>
        /// <param name="netmask"> Netmask, that should be used </param>
        /// <returns> New instance of IPAddress with the network address assigend </returns>
        public static IPAddress GetNetworkAddress(this IPAddress ipAddress, IPAddress netmask)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            if (netmask == null)
                throw new ArgumentNullException("netMask");

            if (ipAddress.AddressFamily != netmask.AddressFamily)
                throw new ArgumentOutOfRangeException("netmask",
                    "Protocoll version of ipAddress and netmask do not match");

            byte[] resultBytes = ipAddress.GetAddressBytes();
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();
            byte[] netmaskBytes = netmask.GetAddressBytes();

            for (int i = 0; i < netmaskBytes.Length; i++)
            {
                resultBytes[i] = (byte)(ipAddressBytes[i] & netmaskBytes[i]);
            }

            return new IPAddress(resultBytes);
        }

        /// <summary>
        ///     Gets the network address for a specified IPAddress and netmask
        /// </summary>
        /// <param name="ipAddress"> IPAddress, for that the network address should be returned </param>
        /// <param name="netmask"> Netmask in CIDR format </param>
        /// <returns> New instance of IPAddress with the network address assigend </returns>
        public static IPAddress GetNetworkAddress(this IPAddress ipAddress, int netmask)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            if ((ipAddress.AddressFamily == AddressFamily.InterNetwork) && ((netmask < 0) || (netmask > 32)))
                throw new ArgumentException("Netmask have to be in range of 0 to 32 on IPv4 addresses", "netmask");

            if ((ipAddress.AddressFamily == AddressFamily.InterNetworkV6) && ((netmask < 0) || (netmask > 128)))
                throw new ArgumentException("Netmask have to be in range of 0 to 128 on IPv6 addresses", "netmask");

            byte[] ipAddressBytes = ipAddress.GetAddressBytes();

            for (int i = 0; i < ipAddressBytes.Length; i++)
            {
                if (netmask >= 8)
                {
                    netmask -= 8;
                }
                else
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        ipAddressBytes[i] &= ReverseBitOrder((byte)~(255 << netmask));
                    }
                    netmask = 0;
                }
            }

            return new IPAddress(ipAddressBytes);
        }

        /// <summary>
        ///     Returns the reverse lookup address of an IPAddress
        /// </summary>
        /// <param name="ipAddress"> Instance of the IPAddress, that should be used </param>
        /// <returns> A string with the reverse lookup address </returns>
        public static string GetReverseLookupAddress(this IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            var res = new StringBuilder();

            byte[] addressBytes = ipAddress.GetAddressBytes();

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                for (int i = addressBytes.Length - 1; i >= 0; i--)
                {
                    res.Append(addressBytes[i]);
                    res.Append(".");
                }
                res.Append("in-addr.arpa");
            }
            else
            {
                for (int i = addressBytes.Length - 1; i >= 0; i--)
                {
                    string hex = addressBytes[i].ToString("x2");
                    res.Append(hex[1]);
                    res.Append(".");
                    res.Append(hex[0]);
                    res.Append(".");
                }

                res.Append("ip6.arpa");
            }

            return res.ToString();
        }

        /// <summary>
        ///     Returns a value indicating whether a ip address is a multicast address
        /// </summary>
        /// <param name="ipAddress"> Instance of the IPAddress, that should be used </param>
        /// <returns> true, if the given address is a multicast address; otherwise, false </returns>
        public static bool IsMulticast(this IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                return ipAddress.GetNetworkAddress(4).Equals(_ipv4MulticastNetworkAddress);
            }
            return ipAddress.GetNetworkAddress(8).Equals(_ipv6MulticastNetworkAddress);
        }

        /// <summary>
        ///     Returns the index for the interface which has the ip address assigned
        /// </summary>
        /// <param name="ipAddress"> The ip address to look for </param>
        /// <returns> The index for the interface which has the ip address assigned </returns>
        public static int GetInterfaceIndex(this IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            IPInterfaceProperties interfaceProperty =
                NetworkInterface.GetAllNetworkInterfaces()
                    .Select(n => n.GetIPProperties())
                    .FirstOrDefault(p => p.UnicastAddresses.Any(a => a.Address.Equals(ipAddress)));

            if (interfaceProperty != null)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    IPv4InterfaceProperties property = interfaceProperty.GetIPv4Properties();
                    if (property != null)
                        return property.Index;
                }
                else
                {
                    IPv6InterfaceProperties property = interfaceProperty.GetIPv6Properties();
                    if (property != null)
                        return property.Index;
                }
            }

            throw new ArgumentOutOfRangeException("ipAddress",
                "The given ip address is not configured on the local system");
        }

        private static byte ReverseBitOrder(byte value)
        {
            byte result = 0;

            for (int i = 0; i < 8; i++)
            {
                result |= (byte)((((1 << i) & value) >> i) << (7 - i));
            }

            return result;
        }
        #endregion

        #region CONCURRENT BAG

        /// <summary>
        /// Sintactic sugar for corr.TryTake(out elem);
        /// </summary>
        /// <param name="corr">Concurrent bag on wich remove the element</param>
        /// <param name="elem">Element to be removed</param>
        public static void Remove<T>(ConcurrentBag<T> corr, T elem)
        {
            corr.TryTake(out elem);
        }

        #endregion

        #region BYTE ARRAY

        public static string ToBase64(this byte[] array)
        {
            return System.Convert.ToBase64String(array);
        }
        #endregion

        #region STRING

        /// <summary>
        /// Check if is a valid credit card number
        /// </summary>
        /// <param name="cc">String to check</param>
        /// 
        public static bool IsValidCreditCard(this string cc)
        {
            int[] deltas = { 0, 1, 2, 3, 4, -4, -3, -2, -1, 0 };
            var checksum = 0;
            var chars = cc.Where(char.IsDigit).ToArray();

            for (var i = chars.Length - 1; i > -1; i--)
            {
                int j = chars[i] - 48;
                checksum += j;
                if (((i - chars.Length) % 2) == 0)
                    checksum += deltas[j];
            }

            return ((checksum % 10) == 0);
        }

        /// <summary>
        /// True if the string ends with given char
        /// </summary>
        /// <param name="str">String to checked</param>
        /// <param name="c">Char to be found</param>
        public static bool EndsWith(this string str, char c)
        {
            return str[str.Length - 1] == c;
        }

        /// <summary>
        /// Replace invalid HTML chars with relative HTML entities
        /// </summary>
        /// <param name="str">String to encoded</param>
        public static string HTMLEncode(this string str)
        {
            return HttpUtility.HtmlEncode(str);
        }

        /// <summary>
        /// Replace HTML entities with relatives UTF-8 chars
        /// </summary>
        /// <param name="str">String to decoded</param>
        public static string HTMLDecode(this string str)
        {
            return HttpUtility.HtmlDecode(str);
        }

        /// <summary>
        /// Remove all HTML tags
        /// </summary>
        /// <param name="str">String to be cleaned</param>
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
        
        /// <summary>
        /// Return a random element from the collection
        /// </summary>
        /// <param name="source">The collection</param>
        public static T Random<T>(this IEnumerable<T> source)
        {
            int c = source.Count();
            return source.Skip(Security.Random(c - 1)).First();
        }

        /// <summary>
        /// Split the collection in batch of N elements
        /// </summary>
        /// <param name="source">Collection to be splitted</param>
        /// <param name="batchSize">Length of the batch</param>
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

        /// <summary>
        /// Apply an action on any elements in the collections
        /// </summary>
        /// <param name="enu">The collection</param>
        /// <param name="batchSize">The action</param>
        public static void ForEach<T>(this IEnumerable<T> enu, Action<T> act)
        {
            foreach (T obj in enu)
            {
                act.Invoke(obj);
            }
        }

        /// <summary>
        /// Sintactic sugar for enu.Concat(new[]{elem})
        /// </summary>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enu, T elem)
        {
        	return enu.Concat(new[]{elem});
        }

        #endregion

        #region ARRAY

        /// <summary>
        /// Sintactic sugar for enu.Concat(new[] { elem }).ToArray()
        /// </summary>
        public static T[] Push<T>(this T[] enu, T elem)
        {
            return enu.Concat(new[] { elem }).ToArray();
        }

        #endregion

        #region DATETIME
        /// <summary>
        /// Convert datetime in number of seconds from 00:00:00 1/1/1970
        /// </summary>
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

        public static void ToBinary(this object obj,Stream stream)
        {
            Binary.Serialize(obj,stream);
        }

        public static byte[] ToBinary(this object obj)
        {
            return Binary.Serialize(obj);
        }

        public static T FromXML<T>(this T obj, string xml)
        {
            return XML.Deserialize<T>(xml);
        }

        public static void ToXML(this object obj,Stream stream)
        {
            XML.Serialize(obj,stream);
        }

        public static string ToXML(this object obj)
        {
            return XML.Serialize(obj);
        }

        public static T FromJSON<T>(this T obj, string json)
        {
            return JSON.Deserialize<T>(json);
        }

        public static void ToJSON(this object obj, Stream stream, bool singlerow = false)
        {
            JSON.Serialize(obj, stream, singlerow);
        }

        public static void ToJSON(this object obj,TextWriter writer, bool singlerow = false)
        {
            JSON.Serialize(obj,writer, singlerow);
        }

        public static string ToJSON(this object obj,bool singlerow=false)
        {
            return JSON.Serialize(obj,singlerow);
        }

        #endregion

        #region METHODINFO

        /// <summary>
        /// Return True if this method has an attribute of type T
        /// </summary>
        public static bool HasAttribute<T>(this MethodInfo type) where T : Attribute
        {
            bool b = type.GetCustomAttributes(false).OfType<T>().Any();
            return b;
        }

        /// <summary>
        /// Return all attributes of type T of this method
        /// </summary>
        public static T[] CustomAttribute<T>(this MethodInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(true).OfType<T>().ToArray();
        }

        #endregion

        #region FIELD INFO
        /// <summary>
        /// Return True if this field has an attribute of type T
        /// </summary>
        public static bool HasAttribute<T>(this FieldInfo type) where T : Attribute
        {
            bool b = type.GetCustomAttributes(false).OfType<T>().Any();
            return b;
        }
        /// <summary>
        /// Return all attributes of type T of this field
        /// </summary>
        public static T[] CustomAttribute<T>(this FieldInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(true).OfType<T>().ToArray();
        }

        #endregion

        #region PROPERTYINFO

        /// <summary>
        /// Retrieve the value of this property for object obj
        /// </summary>
        public static object GetValue(this PropertyInfo pi, object obj)
        {
            return pi.GetGetMethod(true).Invoke(obj, null);
        }

        /// <summary>
        /// Return True if this property has an attribute of type T
        /// </summary>
        public static bool HasAttribute<T>(this PropertyInfo type) where T : Attribute
        {
            bool b = type.GetCustomAttributes(false).OfType<T>().Any();
            return b;
        }

        /// <summary>
        /// Return all attributes of type T of this property
        /// </summary>
        public static T[] CustomAttribute<T>(this PropertyInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(true).OfType<T>().ToArray();
        }

        #endregion

        #region TYPE

        /// <summary>
        /// Return all fields
        /// </summary>
        public static FieldInfo[] GetAllFields(this Type type)
        {
            return type.GetFields();
        }


        /// <summary>
        /// Return the default value of the type
        /// </summary>
        public static object DefaultValue(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// True if this method define an attribute of type T
        /// </summary>
        public static bool HasAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            var b = type.GetCustomAttributes(inherit);
            var c = b.OfType<T>();
            return  c.Any();
        }
        /// <summary>
        /// Return all attributes of type T of this type
        /// </summary>
        public static T[] CustomAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().ToArray();
        }

        /// <summary>
        /// Return all inherited types
        /// </summary>
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

        /// <summary>
        /// True if the type inherit the given one
        /// Runtime equivalent of "is" operator
        /// </summary>
        /// <param name="type">Anchestor to be checked</param>
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

        /// <summary>
        /// True if the type implements the given interface
        /// </summary>
        public static bool Implements(this Type type, Type @interface)
        {
            var ints = type.GetInterfaces();
            return ints.Any(x => x==@interface ||  (x.IsGenericType && x.GetGenericTypeDefinition() == @interface));
        }

        /// <summary>
        /// Create an instance of the type
        /// </summary>
        /// <param name="obj">Consructor parameters.None to use default constructor</param>
        public static object CreateIstance(this Type type, params object[] obj)
        {
            return (obj == null || obj.Length == 0)
                       ? Activator.CreateInstance(type)
                       : Activator.CreateInstance(type, obj);
        }

        #endregion

        #region STREAM
        /// <summary>
        /// Read all bytes into the stream
        /// </summary>
        public static byte[] BinaryRead(this Stream s)
        {
            var b = new byte[s.Length - s.Position];
            s.Read(b, 0, b.Length);
            return b;
        }

        #endregion
    }
}