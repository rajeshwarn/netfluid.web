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
    public class Settings : ConfigurationSection
    {
        [ConfigurationProperty("Timeout", DefaultValue = 300, IsRequired = false)]
        public int Timeout
        {
            get { return (int) this["Timeout"]; }
            set { this["Timeout"] = value; }
        }

        [ConfigurationProperty("LogPath", DefaultValue = "./AppLog.txt", IsRequired = false)]
        public string LogPath
        {
            get { return (string) this["LogPath"]; }
            set { this["LogPath"] = value; }
        }

        [ConfigurationProperty("LogLevel", DefaultValue = LogLevel.Exception, IsRequired = false)]
        public LogLevel LogLevel
        {
            get { return (LogLevel) this["LogLevel"]; }
            set { this["LogLevel"] = value; }
        }

        [ConfigurationProperty("DevMode", DefaultValue = false, IsRequired = false)]
        public bool DevMode
        {
            get { return (bool) this["DevMode"]; }
            set { this["DevMode"] = value; }
        }

        [ConfigurationProperty("MaxPostSize", DefaultValue = 256*1024*1024, IsRequired = true)]
        [IntegerValidator(MinValue = 0, MaxValue = 1024*1024*1024)]
        public int MaxPostSize
        {
            get { return (int) this["MaxPostSize"]; }
            set { this["MaxPostSize"] = value; }
        }

        [ConfigurationProperty("SessionDuration", DefaultValue = 3600, IsRequired = true)]
        [IntegerValidator(MinValue = 30, MaxValue = 1024*1024*1024)]
        public int SessionDuration
        {
            get { return (int) this["SessionDuration"]; }
            set { this["SessionDuration"] = value; }
        }

        [ConfigurationProperty("Interfaces")]
        [ConfigurationCollection(typeof (Interface), AddItemName = "Interface")]
        public Interfaces Interfaces
        {
            get { return (Interfaces) this["Interfaces"]; }
            set { this["Interfaces"] = value; }
        }

        [ConfigurationProperty("PublicFolders")]
        [ConfigurationCollection(typeof (PublicFolder), AddItemName = "PublicFolder")]
        public PublicFolders PublicFolders
        {
            get { return (PublicFolders) this["PublicFolders"]; }
            set { this["PublicFolders"] = value; }
        }
    }
}