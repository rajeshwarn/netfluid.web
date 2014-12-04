using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFluid
{
    public class HttpFile
    {
        public string Name;
        public string FileName;
        public string Extension;
        public string TempFile;

        public void SaveAs(string name, bool overwrite=true)
        {
            if (overwrite && System.IO.File.Exists(name))
                System.IO.File.Delete(name);

            System.IO.File.Move(TempFile, name);
        }

        public string ContentDisposition { get; set; }

        public string ContentType { get; set; }

    }
}
