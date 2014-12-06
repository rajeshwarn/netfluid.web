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
    public class PublicFolderManager: IPublicFolderManager
    {
        IEnumerable<string> folders;

        public PublicFolderManager(string folder)
        {
            folders = new[] {  Path.GetFullPath(folder) };
        }

        public PublicFolderManager(params string[] folders)
        {
            this.folders = folders.Select(Path.GetFullPath);
        }


        public bool TryGetFile(Context cnt)
        {
            var founds = folders.SelectWhere(x =>Path.GetFullPath(Path.Combine(x,cnt.Request.Url)),(d,f)=>File.Exists(f) && f.StartsWith(d));

            if(founds.Any())
            {
                var path = founds.First();
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
