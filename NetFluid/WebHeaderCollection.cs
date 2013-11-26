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
using System.Globalization;
using System.Text;

namespace NetFluid
{
    [Serializable]
    public class WebHeaderCollection
    {
        private readonly Dictionary<string, List<string>> values;

        public WebHeaderCollection()
        {
            values = new Dictionary<string, List<string>>();
        }

        public string this[string index]
        {
            get { return values.ContainsKey(index) ? values[index][0] : string.Empty; }
            set { Append(index, value); }
        }

        public bool Has(string index)
        {
            return values.ContainsKey(index);
        }

        public void Append(string name, string value)
        {
            if (values.ContainsKey(name))
                values[name].Add(value);
            else
                values.Add(name, new List<string>(new[] {value}));
        }

        public void Set(string name, string value)
        {
            if (values.ContainsKey(name))
            {
                values[name].Clear();
                values[name].Add(value);
            }
            else
            {
                values.Add(name, new List<string>(new[] {value}));
            }
        }

        public override string ToString()
        {
            var b = new StringBuilder();

            foreach (var item in values)
            {
                foreach (string sub in item.Value)
                {
                    b.Append(item.Key + ": ");
                    b.Append(sub + "\r\n");
                }
            }
            return b.ToString();
        }
    }
}