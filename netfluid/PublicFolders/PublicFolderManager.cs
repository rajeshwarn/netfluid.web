using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
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
        private readonly MemoryCache _eTagCache;

        public PublicFolderManager()
        {
            _folders = new List<Folder>();
            _eTagCache = MemoryCache.Default;
        }

        /// <summary>
        /// If the requested URI is mapped by one of the pubblic folder, serve and close the context
        /// </summary>
        /// <param name="cnt"></param>
        public void Serve(Context cnt)
        {
            foreach (var item in _folders)
            {
                if (!cnt.Request.Url.StartsWith(item.Uri))
                    continue;

                string path =
                    Path.GetFullPath(item.RealPath +
                                     cnt.Request.Url.Substring(item.Uri.Length)
                                         .Replace('/', Path.DirectorySeparatorChar));

                if (!path.StartsWith(item.RealPath))
                {
                    cnt.Response.StatusCode = StatusCode.BadRequest;
                    cnt.Close();
                    return;
                }

                if (!File.Exists(path))
                    continue;

                var etag = _eTagCache.Get(path) as string;
                if (etag != null)
                {
                    cnt.Response.Headers["ETag"] = "\"" + etag + "\"";
                    if (cnt.Request.Headers.Contains("If-None-Match") &&
                        cnt.Request.Headers["If-None-Match"].Unquote() == etag)
                    {
                        cnt.Response.StatusCode = StatusCode.NotModified;
                        cnt.Close();
                        return;
                    }
                }
                else
                {
                    _eTagCache.Add(path, Security.SHA1Checksum(path), DateTimeOffset.Now + TimeSpan.FromHours(1));
                }
                cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
                cnt.Response.Headers["Expires"] = (DateTime.Now + TimeSpan.FromDays(7)).ToGMT();
                cnt.Response.Headers["Vary"] = "Accept-Encoding";
                cnt.SendHeaders();
                var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs.CopyTo(cnt.OutputStream);
                cnt.Close();
                fs.Close();
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

            foreach (var path in Directory.GetFiles(realPath,"*.*"))
            {
                _eTagCache.Add(path, Security.SHA1Checksum(path), DateTimeOffset.Now + TimeSpan.FromHours(1));
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
    }
}
