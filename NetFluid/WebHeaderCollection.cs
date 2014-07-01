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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NetFluid
{
    /// <summary>
    /// Contains HTTP request or response headers 
    /// </summary>
    [Serializable]
    public class WebHeaderCollection:IEnumerable<WebHeader>
    {
        private readonly Dictionary<string, WebHeader> values;

        public WebHeaderCollection()
        {
            values = new Dictionary<string, WebHeader>();
        }

        public WebHeaderCollection(IEnumerable<WebHeader> collection)
        {
            values = new Dictionary<string, WebHeader>();

            foreach (var item in collection)
            {
                values.Add(item.Name.ToLowerInvariant(), item);
            }
        }

        /// <summary>
        /// Return header from name
        /// </summary>
        /// <param name="index">header name</param>
        /// <returns>header</returns>
        public string this[string index]
        {
            get
            {
                string lower = index.ToLowerInvariant();

                return values.ContainsKey(lower) ? values[lower].ToString() : string.Empty;
            }
            set
            {
                string lower = index.ToLowerInvariant();

                if (values.ContainsKey(lower))
                    values[lower].Add(value);
                else
                    values.Add(lower, new WebHeader(index, value));
            }
        }

        /// <summary>
        /// True if contain the header
        /// </summary>
        /// <param name="index">name of the header</param>
        /// <returns></returns>
        public bool Contains(string index)
        {
            return values.ContainsKey(index.ToLowerInvariant());
        }

        /// <summary>
        /// Add a new header to the collection.If header already exist a new one is appended
        /// </summary>
        /// <param name="name">name of the header</param>
        /// <param name="value">value of the header</param>
        public void Append(string name, string value)
        {
            string lower = name.ToLowerInvariant();

            if (values.ContainsKey(lower))
                values[lower].Add(value);
            else
                values.Add(lower, new WebHeader(name, value));
        }

        /// <summary>
        ///  Add a new header to the collection.If header already exist this one is appended
        /// </summary>
        /// <param name="header">header to append</param>
        public void Append(WebHeader header)
        {
            string lower = header.Name.ToLowerInvariant();

            if (values.ContainsKey(lower))
                values[lower].Add(header);
            else
                values.Add(lower, header);
        }

        /// <summary>
        ///  Add a new header to the collection.If header already exist is replaced
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Set(string name, string value)
        {
            values[name.ToLowerInvariant()] = new WebHeader(name, value);
        }

        public IEnumerator<WebHeader> GetEnumerator()
        {
            return values.Values.GetEnumerator();
        }

        public override string ToString()
        {
            var b = new StringBuilder();

            foreach (var item in values)
                foreach (var sub in item.Value)
                    b.Append(item.Key + ": " + sub + "\r\n");
            return b.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Delete all headers
        /// </summary>
        public void Clear()
        {
            values.Clear();
        }
    }
}