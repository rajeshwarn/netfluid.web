using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace NetFluid
{
    public class PublicFolder : IPublicFolder
    {
        public string VirtualPath;
        public string RealPath;

        public bool TryGetFile(Context cnt)
        {
            if (cnt.Request.Url.LocalPath.StartsWith(VirtualPath))
            {
                var subUrl = cnt.Request.Url.LocalPath.Substring(VirtualPath.Length).Replace('/', Path.DirectorySeparatorChar);
                var fpath = Path.GetFullPath(RealPath);

                if (subUrl[0] != Path.DirectorySeparatorChar)
                    subUrl = Path.DirectorySeparatorChar + subUrl;

                if (fpath.EndsWith(Path.DirectorySeparatorChar))
                    fpath = fpath.Substring(0, fpath.Length - 1);

                var path = Path.GetFullPath(fpath + subUrl);

                //security check
                if (!File.Exists(path) || !path.StartsWith(fpath))
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
                catch(HttpListenerException)
                {
                    cnt.Close();
                }
                catch (Exception ex)
                {
                    cnt.Close();
                }
                return true;
            }
            return false;
        }
    }
}
