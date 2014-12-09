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
    public class DefaultPublicFolderManager: List<PublicFolder>, IPublicFolderManager
    {
        public bool TryGetFile(Context cnt)
        {
            var found = this.Where(x=>cnt.Request.Url.StartsWith(x.VirtualPath)).FirstOrDefault();

            if(found != null)
            {
                var subUrl = cnt.Request.Url.Substring(found.VirtualPath.Length);
                var fpath =  Path.GetFullPath(found.RealPath);
                var path = Path.GetFullPath(fpath + subUrl.Replace('/',Path.DirectorySeparatorChar));

                //security check
                if (!path.StartsWith(fpath))
                    return false;

                cnt.Response.ContentType = MimeTypes.GetType(path);
                cnt.Response.Headers["Expires"] = (DateTime.Now + TimeSpan.FromDays(7)).ToGMT();
                cnt.SendHeaders();
                var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs.CopyTo(cnt.OutputStream);
                cnt.Close();
                fs.Close();

                return true;
            }
            return false;
        }
    }
}
