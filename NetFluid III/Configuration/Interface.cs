﻿// ********************************************************************************************************
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
using System.Configuration;

namespace NetFluid
{
    public class Interface : ConfigurationElement
    {
        [ConfigurationProperty("IP", DefaultValue = "127.0.0.1", IsRequired = true)]
        public String IP
        {
            get { return this["IP"] as String; }
            set { this["IP"] = value; }
        }

        [ConfigurationProperty("Port", DefaultValue = "8080", IsRequired = true)]
        [IntegerValidator(MinValue = 1, MaxValue = 65000)]
        public int Port
        {
            get { return (int) this["Port"]; }
            set { this["Port"] = value; }
        }

        [ConfigurationProperty("Certificate", DefaultValue = "", IsRequired = false)]
        public String Certificate
        {
            get { return this["Certificate"] as String; }
            set { this["Certificate"] = value; }
        }
    }

    public class Interfaces : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Interface();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var i = element as Interface;
            return (i.Certificate == "" ? "http://" : "https://") + i.IP + ":" + i.Port;
        }
    }
}