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
using System.Security.Cryptography;
using System.Text;

namespace NetFluid
{
    public static class Security
    {
        static object RandomLocker;
        static Random RandomGenerator;
        
        static Security()
        {
            RandomLocker = new object();
            RandomGenerator = new Random();
        }

        public static string SecWebSocketAccept(string key)
        {
            const String MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key + MagicKEY));


            return Convert.ToBase64String(sha1Hash);
        }

        public static string SHA1Checksum(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                var checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        public static byte[] Base64Decode(string str)
        {
            return Convert.FromBase64String(str);
        }

        public static string Base64Encode(byte[] toEncode)
        {
            return Convert.ToBase64String(toEncode);
        }

        public static string Sha1(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var cryptoTransformSha1 = new SHA1CryptoServiceProvider();
            string hash = BitConverter.ToString(cryptoTransformSha1.ComputeHash(buffer)).Replace("-", "");
            return hash;
        }

        public static string TempFile
        {
            get
            {
                //Path.GetTempFileName() SOMETIMES RETURN THE SAME FILENAME ON TWO CALLINGS
                string p;
                do p = Path.Combine(Path.GetTempPath(), UID()); while (File.Exists(p));
                return p;
            }
        }

        /// <summary>
        /// Return a random value
        /// </summary>
        /// <returns></returns>
        public static int Random()
        {
            int k;
            lock (RandomLocker)
            {
                k = RandomGenerator.Next();
                RandomGenerator = new Random(k);
            }
            return k;
        }

        /// <summary>
        /// Return a random value from to 0 to max
        /// </summary>
        /// <param name="max">maximum value</param>
        /// <returns></returns>
        public static int Random(int max)
        {
            int k;
            lock (RandomLocker)
            {
                k = RandomGenerator.Next(max);
                RandomGenerator = new Random(k);
            }
            return k;
        }

        /// <summary>
        /// Return a random value from to min to max
        /// </summary>
        /// <param name="min">minimun value</param>
        /// <param name="max">maximum value</param>
        /// <returns></returns>
        public static int Random(int min, int max)
        {
            int k;
            lock (RandomLocker)
            {
                k = RandomGenerator.Next(min,max);
                RandomGenerator = new Random(k);
            }
            return k;
        }

        public static string UID()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}