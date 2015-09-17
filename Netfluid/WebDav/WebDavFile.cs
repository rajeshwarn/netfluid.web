using System;
using System.IO;
using WebDAVSharp.Server.Stores;

namespace Netfluid.WebDav
{
    class WebDavFile : IWebDavStoreDocument
    {
        string path;

        public WebDavFile(string f)
        {
            path = f;
        }

        public DateTime CreationDate
        {
            get
            {
                return File.GetCreationTime(path);
            }
        }

        public string Etag
        {
            get
            {
                return Guid.NewGuid().ToString();
            }
        }

        public int Hidden
        {
            get
            {
                return 0;
            }
        }

        public bool IsCollection
        {
            get
            {
                return false;
            }
        }

        public string ItemPath
        {
            get
            {
                return "/" + System.IO.Path.GetFileName(path);
            }
        }

        public string MimeType
        {
            get
            {
                return "text/html";
            }
        }

        public DateTime ModificationDate
        {
            get
            {
                return (new FileInfo(path)).LastWriteTime;
            }
        }

        public string Name
        {
            get
            {
                return Path.GetFileName(path);
            }

            set
            {
                File.Move(path, Path.Combine(Path.GetDirectoryName(path), value));
            }
        }

        public IWebDavStoreCollection ParentCollection
        {
            get
            {
                return new WebDavDirectory(Path.GetDirectoryName(path));
            }
        }

        public long Size
        {
            get
            {
                FileInfo d = new FileInfo(path);
                return d.Length;
            }
        }

        public Stream OpenReadStream()
        {
            return File.Open(path, FileMode.Open);
        }

        public Stream OpenWriteStream(bool append)
        {
            return File.Open(path, append ? FileMode.Append : FileMode.OpenOrCreate);
        }
    }
}
    
