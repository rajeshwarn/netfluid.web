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
    public class PublicFolderManager:IPublicFolderManager
    {
        public void Serve(Context cnt)
        {
            throw new NotImplementedException();
        }

        public void Add(PublicFolder folder)
        {
            throw new NotImplementedException();
        }

        public void Add(string uriPath, string realPath)
        {
            throw new NotImplementedException();
        }

        public bool TryGetFile(Context cnt)
        {
            /*cnt.Response.ContentType = MimeTypes.GetType(cnt.Request.Url);
            cnt.Response.Headers["Expires"] = (DateTime.Now + TimeSpan.FromDays(7)).ToGMT();
            cnt.SendHeaders();
            var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.CopyTo(cnt.OutputStream);
            cnt.Close();
            fs.Close();*/
            return true;
        }
    }
}
