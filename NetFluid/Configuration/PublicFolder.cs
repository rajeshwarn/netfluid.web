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


using System.Configuration;

namespace NetFluid
{
    public class PublicFolder : ConfigurationElement
    {
        [ConfigurationProperty("RealPath", DefaultValue = "./public", IsRequired = true)]
        public string RealPath
        {
            get { return this["RealPath"] as string; }
            set { this["RealPath"] = value; }
        }

        [ConfigurationProperty("UriPath", DefaultValue = "/", IsRequired = true)]
        public string UriPath
        {
            get { return this["UriPath"] as string; }
            set { this["UriPath"] = value; }
        }

        [ConfigurationProperty("Host", DefaultValue = "", IsRequired = false)]
        public string Host
        {
            get { return (string) this["Host"]; }
            set { this["Host"] = value; }
        }

        [ConfigurationProperty("Immutable", DefaultValue = false, IsRequired = false)]
        public bool Immutable
        {
            get { return (bool) this["Immutable"]; }
            set { this["Immutable"] = value; }
        }
    }

    public class PublicFolders : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PublicFolder();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var i = element as PublicFolder;
            return i.RealPath + ":" + i.UriPath;
        }
    }
}