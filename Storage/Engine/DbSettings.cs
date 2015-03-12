using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;

namespace FluidDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Can be used as:
    /// * If only a word - get from App.Config
    /// * If is a path - use all parameters as default
    /// * Otherwise, is name=value collection
    /// </summary>
    public class DbSettings
    {
        public DbSettings(string filename)
        {
            // Read connection string parameters with default value
            Timeout = new TimeSpan(0, 1, 0);
            Filename = Path.GetFullPath(filename);
            JournalEnabled = true;
            UserVersion = 1;
            // generate journal path/filename
            JournalFilename = Path.Combine(Path.GetDirectoryName(Filename),
                Path.GetFileNameWithoutExtension(Filename) + "-journal" +
                Path.GetExtension(Filename));
        }

        /// <summary>
        /// Path of filename (no default - required key)
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Default Timeout connection to wait for unlock (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Supports recovery mode if a fail during write pages to disk
        /// </summary>
        public bool JournalEnabled { get; set; }

        /// <summary>
        /// Internal file for journal
        /// </summary>
        internal string JournalFilename { get; set; }

        /// <summary>
        /// Define, in connection string, the user database version. When you increse this value
        /// LiteEngine will run OnUpdate method for each new version. If defined, must be >= 1. Default: 1
        /// </summary>
        public int UserVersion { get; set; }
    }
}
