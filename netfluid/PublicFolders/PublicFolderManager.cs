using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using NetFluid.Collections;
using NetFluid.HTTP;

namespace NetFluid
{
    /// <summary>
    /// Default Netfluid public folder manager. Expose file-downloadable folder to the client and close the context.
    /// </summary>
    public class PublicFolderManager : IPublicFolderManager
    {
        class Folder
        {
            public string Id;
            public string Uri;
            public string RealPath;
        }

        private readonly List<Folder> _folders;
        private readonly AutoCache<string> uriCache;
        private readonly AutoCache<string> etagCache; 

        public PublicFolderManager()
        {
            _folders = new List<Folder>();

            uriCache = new AutoCache<string>
            {
                Expiration = TimeSpan.FromHours(2),
                Get = ToPath
            };

            etagCache = new AutoCache<string>
            {
                Expiration = TimeSpan.FromHours(2),
                Get = Security.SHA1Checksum
            };
        }

        /// <summary>
        /// If the requested URI is mapped by one of the pubblic folder, serve and close the context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            try
            {
                var path = uriCache[cnt.Request.Url];

                if (path == null) return;

                var etag = etagCache[path];
                cnt.Response.Headers["ETag"] = "\"" + etag + "\"";
                if (cnt.Request.Headers.Contains("If-None-Match") && cnt.Request.Headers["If-None-Match"].Unquote() == etag)
                {
                    cnt.Response.StatusCode = StatusCode.NotModified;
                    cnt.Close();
                    return;
                }

                cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
                cnt.Response.Headers["Expires"] = (DateTime.Now + TimeSpan.FromDays(7)).ToGMT();

                cnt.SendHeaders();
                var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs.CopyTo(cnt.OutputStream);
                cnt.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
            if (!Directory.Exists(realPath))
            {
                Engine.Logger.Log(LogLevel.Error, "Failed to add public folder, directory is missing " + realPath);
                return;
            }

            var f = Path.GetFullPath(realPath);

            if (f.Last() != Path.DirectorySeparatorChar)
                f = f + Path.DirectorySeparatorChar;

            var folder = new Folder {Id = id, RealPath = f, Uri = uriPath};
            lock (_folders)
            {
                _folders.Add(folder);
            }
        }

        /// <summary>
        /// Remove the public folder
        /// </summary>
        /// <param name="id"></param>
        public void Remove(string id)
        {
            _folders.RemoveAll(x => x.Id == id);
        }

        /// <summary>
        /// Return all managed directories
        /// </summary>
        public IEnumerable<string> Directories
        {
            get { return _folders.Select(x => x.RealPath); }
        }

        /// <summary>
        /// Return all managed files
        /// </summary>
        public IEnumerable<string> Files
        {
            get { return _folders.SelectMany(x => Directory.GetFiles(x.RealPath,"*.*",SearchOption.AllDirectories)); }
        }

        public IEnumerable<string> URIs
        {
            get
            {
                return
                    _folders.SelectMany(x => Directory.GetFiles(x.RealPath, "*.*", SearchOption.AllDirectories))
                        .Select(ToUri);
            }
        }

        public string ToPath(string uri)
        {
            foreach (var item in _folders)
            {
                if (!uri.StartsWith(item.Uri))
                    continue;

                var path = Path.GetFullPath(item.RealPath + uri.Substring(item.Uri.Length).Replace('/', Path.DirectorySeparatorChar));

                if (!path.StartsWith(item.RealPath) || !File.Exists(path))
                    continue;

                return path;
            }
            return null;
        }

        public string ToUri(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var folder = _folders.FirstOrDefault(x => x.RealPath.StartsWith(fullPath));

            if (folder != null)
            {
                return folder.Uri + '/' + fullPath.Substring(folder.RealPath.Length).Replace(Path.DirectorySeparatorChar, '/');
            }
            return null;
        }

        public Stream OpenFile(string uri)
        {
            var p = ToPath(uri);

            return p != null ? new FileStream(p,FileMode.Open,FileAccess.Read) : null;
        }
    }
}
