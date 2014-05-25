using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetFluid
{
    /// <summary>
    /// Load the entire public folder in memory. Ideal for small immutable folder like css and js.
    /// Any changes to files is ignored
    /// </summary>
    public class InMemoryPublicFolders:IPublicFolderManager
    {
        class InMemoryFile
        {
            public string Path;
            public byte[] Data;
            public string FolderId;
        }

        private readonly List<string> directories; 
        private readonly Dictionary<string, InMemoryFile> _immutableData;

        public InMemoryPublicFolders()
        {
            _immutableData = new Dictionary<string, InMemoryFile>();
            directories = new List<string>();
        }

        /// <summary>
        /// If the requested URI is mapped by one of the pubblic folder, serve and close the context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            InMemoryFile content;
            if (_immutableData.TryGetValue(cnt.Request.Url, out content))
            {
                //Answer yes without takingcare of the value
                if (cnt.Request.Headers.Contains("If-None-Match"))
                {
                    cnt.Response.StatusCode = StatusCode.NotModified;
                    return;
                }

                cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
                cnt.Response.Headers["Cache-Control"] = "max-age=29030400";
                cnt.Response.Headers["Last-Modified"] = DateTime.MinValue.ToString("r");
                cnt.Response.Headers["Vary"] = "Accept-Encoding";

                //Fake ETag for immutable files
                cnt.Response.Headers["ETag"] = Security.UID();
                cnt.SendHeaders();
                cnt.OutputStream.Write(content.Data, 0, content.Data.Length);
                cnt.Close();
                return;
            }
        }

        /// <summary>
        /// Add  a file-donwloadable folder
        /// </summary>
        /// <param name="folder"></param>
        public void Add(PublicFolder folder)
        {
            Add(folder.Id, folder.UriPath, folder.RealPath);
        }


        /// <summary>
        /// Add a file-downloadable folder
        /// </summary>
        /// <param name="id">Friendly name for the folder</param>
        /// <param name="uriPath">URI to map</param>
        /// <param name="realPath">Physical path</param>
        public void Add(string id, string uriPath, string realPath)
        {
            var m = Path.GetFullPath(realPath);

            lock (directories)
            {
                directories.Add(m);
            }

            var start = uriPath.EndsWith('/') ? uriPath : uriPath + "/";

            if (!Directory.Exists(m))
            {
                Engine.Logger.Log(LogLevel.Error, "Failed to add public folder, directory is missing " + realPath);
                return;
            }

            foreach (var x in Directory.GetFiles(m, "*.*", SearchOption.AllDirectories))
            {
                var s = x.Substring(m.Length).Replace(Path.DirectorySeparatorChar, '/');

                if (s[0] == '/')
                    s = s.Substring(1);

                string fileUri = start + s;
                if (!_immutableData.ContainsKey(fileUri))
                {
                    var f = new InMemoryFile
                    {
                        Data = File.ReadAllBytes(x),
                        FolderId = id,
                        Path = x
                    };
                    _immutableData.Add(fileUri, f);
                }
                else
                {
                    Engine.Logger.Log(LogLevel.Warning,"Public folder URI mapping conflict "+_immutableData[fileUri].Path+" "+x);
                }
            }
        }

        /// <summary>
        /// Remove the public folder
        /// </summary>
        /// <param name="id"></param>
        public void Remove(string id)
        {
            var tbr = _immutableData.Where(x => x.Value.FolderId == id).Select(x => x.Key).ToArray();

            lock (_immutableData)
            {
                tbr.ForEach(x=>_immutableData.Remove(x));
            }
        }

        /// <summary>
        /// Return all managed directories
        /// </summary>
        public IEnumerable<string> Directories
        {
            get { return directories; }
        }

        /// <summary>
        /// Return all managed files
        /// </summary>
        public IEnumerable<string> Files
        {
            get { return _immutableData.Values.Select(x => x.Path); }
        }
    }
}
