using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using WebDAVSharp.Server.Stores;

namespace Netfluid.WebDav
{

    class WebDavDirectory : IWebDavStoreCollection
    {
        string path;

        string UriToPath(string uri)
        {
            if (uri[0] == '/') uri = "." + uri;

            var full = Path.GetFullPath(Path.Combine(path,uri));

            return full.StartsWith(path) ? full : null;
        }

        public WebDavDirectory(string dir)
        {
            path = Path.GetFullPath(dir);
        }

        public DateTime CreationDate
        {
            get
            {
                return Directory.GetCreationTime(path);
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
                return true;
            }
        }

        public string ItemPath
        {
            get
            {
                return path;
            }
        }

        public IEnumerable<IWebDavStoreItem> Items
        {
            get
            {
                return Directory.GetDirectories(path)
                         .Select<string,IWebDavStoreItem>(x => new WebDavDirectory(x))
                         .Concat(Directory.GetFiles(path).Select(x => new WebDavFile(x)));
            }
        }

        public DateTime ModificationDate
        {
            get
            {
                return Directory.GetLastWriteTime(path);
            }
        }

        public string Name { get; set; }

        public IWebDavStoreCollection ParentCollection
        {
            get
            {
                return new WebDavDirectory(Path.GetDirectoryName(path));
            }
        }


        public IWebDavStoreItem MoveItemHere(string source, string destinationName)
        {
            var from = UriToPath(source);
            var to = UriToPath(destinationName);

            if(!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            {
                if (Directory.Exists(from))
                {
                    Directory.Move(from, to);
                    return new WebDavDirectory(from);
                }

                if(File.Exists(from))
                {
                    File.Move(from, to);
                    return new WebDavFile(from);
                }
            }
            return null;
        }

        public IWebDavStoreItem CopyItemHere(string source, string destinationName)
        {
            throw new NotImplementedException();
        }

        public IWebDavStoreCollection CreateCollection(string name)
        {
            var dir = Path.Combine(path, name);
            Directory.CreateDirectory(dir);
            return new WebDavDirectory(dir);
        }

        public IWebDavStoreDocument CreateDocument(string name)
        {
            return new WebDavFile(Path.Combine(path, name));
        }

        public void Delete(string item)
        {
            var el = Path.Combine(path, item);

            if (Directory.Exists(el)) Directory.Delete(el);
            if (File.Exists(el)) Directory.Delete(el);
        }

        public IWebDavStoreItem GetItemByName(string name)
        {
            if (name == "/") return this;

            var kk = Path.Combine(path,name.Substring(1));

            if (Directory.Exists(kk)) return new WebDavDirectory(kk);
            if (File.Exists(kk)) return new WebDavFile(kk);

            return null;
        }
    }
}
