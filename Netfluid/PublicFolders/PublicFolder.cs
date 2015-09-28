using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Netfluid
{
    public class PublicFolder : IPublicFolder
    {
        public string VirtualPath;
        public string RealPath;

        string UriToPath(string uri)
        {
            if (uri == "/") return uri;

            var subUrl = uri.Substring(VirtualPath.Length).Replace('/', Path.DirectorySeparatorChar);
            var fpath = Path.GetFullPath(RealPath);

            if (string.IsNullOrEmpty(subUrl) || subUrl[0] != Path.DirectorySeparatorChar)
                subUrl = Path.DirectorySeparatorChar + subUrl;

            if (fpath.EndsWith(Path.DirectorySeparatorChar))
                fpath = fpath.Substring(0, fpath.Length - 1);

            return Path.GetFullPath(fpath + subUrl);
        }

        public bool Map(Context cnt)
        {
            var path = UriToPath(cnt.Request.Url.LocalPath);

            //security check
            if (!File.Exists(path))
                return false;

            if (!path.StartsWith(Path.GetFullPath(RealPath)))
                return false;

            return true;
        }

        public bool TryGetFile(Context cnt)
        {
            if (cnt.Request.Url.LocalPath.StartsWith(VirtualPath))
            {
                var path = UriToPath(cnt.Request.Url.LocalPath);

                //security check
                if (!File.Exists(path) || !path.StartsWith(Path.GetFullPath(RealPath)))
                    return false;

                cnt.Response.ContentType = MimeTypes.GetType(path);
                cnt.Response.Headers["Expires"] = (DateTime.Now + TimeSpan.FromDays(7)).ToGMT();

                try
                {
                    var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    fs.CopyTo(cnt.Response.OutputStream);
                    cnt.Close();
                    fs.Close();
                }
                catch (Exception)
                {
                    cnt.Close();
                }
                return true;
            }
            return false;
        }
    }
}
