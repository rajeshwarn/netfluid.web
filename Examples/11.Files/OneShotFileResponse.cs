using NetFluid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _11.Files
{
    public class OneShotFileResponse : IResponse
    {
        public OneShotFileResponse(string filepath)
        {
            Path = System.IO.Path.GetFullPath(filepath);
            FileName = System.IO.Path.GetFileName(Path);
            MimeType = MimeTypes.GetType(Path);
        }

        public string FileName { get; set; }
        public string Path { get; set; }
        public string MimeType { get; set; }

        /// <summary>
        /// This method is called by the engine after our web app logic
        /// </summary>
        /// <param name="cnt"></param>
        public void SendResponse(Context cnt)
        {
            cnt.Response.Headers["Content-Disposition"] = "attachment; filename=\"" + FileName + "\"";
            cnt.Response.ContentType = MimeType;
            cnt.SendHeaders();

            var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.CopyTo(cnt.OutputStream);
            fs.Close();

            Task.Factory.StartNew(()=>File.Delete(Path));
        }
    }
}
