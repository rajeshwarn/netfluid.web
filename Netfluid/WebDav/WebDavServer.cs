using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using WebDAVSharp.Server;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Utilities;

namespace Netfluid.WebDav
{
    class WebDavServer
    {
        IWebDavStoreCollection Store;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string GetTimeoutHeader(HttpListenerRequest request)
        {
            // get the value of the timeout header as a string
            string timeout = request.Headers["Timeout"];

            // check if the string is valid or not infinity
            // if so, try to parse it to an int
            if (!String.IsNullOrEmpty(timeout) && !timeout.Equals("infinity") &&
                !timeout.Equals("Infinite, Second-4100000000"))
                return timeout;
            // else, return the timeout value as if it was requested to be 4 days
            return "Second-345600";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetDepthHeader(HttpListenerRequest request)
        {
            // get the value of the depth header as a string
            string depth = request.Headers["Depth"];

            // check if the string is valid or not infinity
            // if so, try to parse it to an int
            if (String.IsNullOrEmpty(depth) || depth.Equals("infinity"))
                return -1;
            int value;
            if (!int.TryParse(depth, out value))
                return -1;
            if (value == 0 || value == 1)
                return value;
            // else, return the infinity value
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool GetOverwriteHeader(HttpListenerRequest request)
        {
            // get the value of the Overwrite header as a string
            string overwrite = request.Headers["Overwrite"];

            // check if the string is valid and if it equals T
            return overwrite != null && overwrite.Equals("T");
            // else, return false
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SendSimpleResponse(Context context, int statusCode = StatusCode.Ok)
        {
            context.Response.StatusCode = statusCode;
            context.Response.Close();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        XmlDocument ResponseDocument(Context context, bool propname, List<IWebDavStoreItem> _webDavStoreItems, List<WebDavProperty> _requestedProperties)
        {
            // Create the basic response XmlDocument
            XmlDocument responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\"?><D:multistatus xmlns:D=\"DAV:\"></D:multistatus>";
            responseDoc.LoadXml(responseXml);

            // Generate the manager
            XmlNamespaceManager manager = new XmlNamespaceManager(responseDoc.NameTable);
            manager.AddNamespace("D", "DAV:");
            manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
            manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
            manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

            int count = 0;

            foreach (IWebDavStoreItem webDavStoreItem in _webDavStoreItems)
            {
                // Create the response element
                WebDavProperty responseProperty = new WebDavProperty("response", "");
                XmlElement responseElement = responseProperty.ToXmlElement(responseDoc);

                // The href element
                var result = count == 0 ? context.Request.Url.LocalPath : context.Request.Url.LocalPath + "/" + webDavStoreItem.Name;

                WebDavProperty hrefProperty = new WebDavProperty("href", result);
                responseElement.AppendChild(hrefProperty.ToXmlElement(responseDoc));
                count++;

                // The propstat element
                WebDavProperty propstatProperty = new WebDavProperty("propstat", "");
                XmlElement propstatElement = propstatProperty.ToXmlElement(responseDoc);

                // The prop element
                WebDavProperty propProperty = new WebDavProperty("prop", "");
                XmlElement propElement = propProperty.ToXmlElement(responseDoc);

                foreach (WebDavProperty davProperty in _requestedProperties)
                {
                    propElement.AppendChild(PropChildElement(davProperty, responseDoc, webDavStoreItem, propname));
                }

                // Add the prop element to the propstat element
                propstatElement.AppendChild(propElement);

                // The status element
                WebDavProperty statusProperty = new WebDavProperty("status", "HTTP/1.1 " + context.Response.StatusCode);
                propstatElement.AppendChild(statusProperty.ToXmlElement(responseDoc));

                // Add the propstat element to the response element
                responseElement.AppendChild(propstatElement);

                // Add the response element to the multistatus element
                responseDoc.DocumentElement.AppendChild(responseElement);
            }

            return responseDoc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        XmlElement PropChildElement(WebDavProperty webDavProperty, XmlDocument xmlDocument, IWebDavStoreItem iWebDavStoreItem, bool isPropname)
        {
            // If Propfind request contains a propname element
            if (isPropname)
            {
                webDavProperty.Value = String.Empty;
                return webDavProperty.ToXmlElement(xmlDocument);
            }
            // If not, add the values to webDavProperty
            webDavProperty.Value = GetWebDavPropertyValue(iWebDavStoreItem, webDavProperty);
            XmlElement xmlElement = webDavProperty.ToXmlElement(xmlDocument);

            // If the webDavProperty is the resourcetype property
            // and the webDavStoreItem is a collection
            // add the collection XmlElement as a child to the xmlElement
            if (webDavProperty.Name != "resourcetype" || !iWebDavStoreItem.IsCollection)
                return xmlElement;

            WebDavProperty collectionProperty = new WebDavProperty("collection", "");
            xmlElement.AppendChild(collectionProperty.ToXmlElement(xmlDocument));
            return xmlElement;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string GetWebDavPropertyValue(IWebDavStoreItem webDavStoreItem, WebDavProperty davProperty)
        {
            switch (davProperty.Name)
            {
                case "creationdate":
                    return webDavStoreItem.CreationDate.ToUniversalTime().ToString("s") + "Z";
                case "displayname":
                    return webDavStoreItem.Name;
                case "getcontentlanguage":
                    // still to implement !!!
                    return String.Empty;
                case "getcontentlength":
                    return (!webDavStoreItem.IsCollection ? "" + ((IWebDavStoreDocument)webDavStoreItem).Size : "");
                case "getcontenttype":
                    return (!webDavStoreItem.IsCollection ? "" + ((IWebDavStoreDocument)webDavStoreItem).MimeType : "");
                case "getetag":
                    return (!webDavStoreItem.IsCollection ? "" + ((IWebDavStoreDocument)webDavStoreItem).Etag : "");
                case "getlastmodified":
                    return webDavStoreItem.ModificationDate.ToUniversalTime().ToString("R");
                case "lockdiscovery":
                    // still to implement !!!
                    return String.Empty;
                case "resourcetype":
                    return "";
                case "supportedlock":
                    // still to implement !!!
                    return "";
                case "ishidden":
                    return "" + webDavStoreItem.Hidden;
                default:
                    return string.Empty;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static List<IWebDavStoreItem> GetWebDavStoreItems(IWebDavStoreItem iWebDavStoreItem, int depth)
        {
            //FIXME
            //Logger _log = LogManager.GetCurrentClassLogger();
            List<IWebDavStoreItem> list = new List<IWebDavStoreItem>();

            //IWebDavStoreCollection
            // if the item is a collection
            IWebDavStoreCollection collection = iWebDavStoreItem as IWebDavStoreCollection;
            if (collection != null)
            {
                list.Add(collection);
                if (depth == 0)
                    return list;
                foreach (IWebDavStoreItem item in collection.Items)
                {
                    try
                    {
                        list.Add(item);
                    }
                    catch (Exception)
                    {
                        //_log.Debug(ex.Message + "\r\n" + ex.StackTrace);
                    }
                }
                return list;
            }
            // if the item is not a document, throw conflict exception
            if (!(iWebDavStoreItem is IWebDavStoreDocument))
                throw new WebDavConflictException();

            // add the item to the list
            list.Add(iWebDavStoreItem);

            return list;
        }

        public WebDavServer(WebDavDirectory webDavDirectory, NetfluidHost host, string mountPoint)
        {
            Store = webDavDirectory;

            var mm = mountPoint + ".*";
            host.Routes["COPY", mm] = Route.New<Context>(Copy);
            host.Routes["DELETE", mm] = Route.New<Context>(Delete);
            host.Routes["GET", mm] = Route.New<Context>(Get);
            host.Routes["HEAD", mm] = Route.New<Context>(Head);
            host.Routes["MKCOL", mm] = Route.New<Context>(MkCol);
            host.Routes["MOVE", mm] = Route.New<Context>(Move);
            host.Routes["OPTIONS", mm] = Route.New<Context>(Options);
            host.Routes["PUT", mm] = Route.New<Context>(Put);
            host.Routes["UNLOCK", mm] = Route.New<Context>(Unlock);
            host.Routes["PROPPATCH", mm] = Route.New<Context>(PropPatch);
            host.Routes["PROPFIND", mm] = Route.New<Context>(PropFind);
            host.Routes["LOCK", mm] = Route.New<Context>(Lock);
        }

        public void Process(Context cnt)
        {
            cnt.Response.AppendHeader("DAV", "1,2,1#extend");
        }

        void Copy(Context context)
        {
            var source = Store.GetItemByName(context.Request.Url.LocalPath);
            var destination = Store.GetItemByName(context.Request.Headers["Destination"]);
            bool isNew = true;


            if (destination != null)
            {
                if (!GetOverwriteHeader(context.Request))
                    throw new WebDavPreconditionFailedException();
                isNew = false;
            }

            ///fixme
            Store.CopyItemHere(context.Request.Url.LocalPath, destination.Name);

            SendSimpleResponse(context,isNew ? StatusCode.Created : StatusCode.NoContent);
        }

        void Delete(Context cnt)
        {
            Store.Delete(cnt.Request.Url.LocalPath);
            SendSimpleResponse(cnt);
        }

        void Get(Context context)
        {
            var item = Store.GetItemByName(context.Request.Url.LocalPath); 

            if (item is IWebDavStoreCollection)
            {
                var dir = item as IWebDavStoreCollection;
                var s = dir.Items.Select(x => "<a href=\"" + x.ItemPath + "\">"+x.Name+"</a>");
                context.Writer.Write(string.Join(" ",s));
                context.Close();
                return;
            }

            var doc = item as IWebDavStoreDocument;
            long docSize = doc.Size;
            if (docSize == 0)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentLength64 = 0;
            }

            using (Stream stream = doc.OpenReadStream())
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;

                if (docSize > 0)
                    context.Response.ContentLength64 = docSize;

                byte[] buffer = new byte[4096];
                int inBuffer;
                while ((inBuffer = stream.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, inBuffer);
            }
            context.Response.Close();
        }

        void Head(Context context)
        {
            // Get the item from the collection
            var item = Store.GetItemByName(context.Request.Url.LocalPath);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentLength64 = 0;
            context.Response.AppendHeader("Content-Type", "text/html");
            context.Response.AppendHeader("Last-Modified", item.ModificationDate.ToUniversalTime().ToString("R"));

            context.Response.Close();
        }

        void MkCol(Context cnt)
        {
            Store.CreateCollection(cnt.Request.Url.LocalPath);
            SendSimpleResponse(cnt,StatusCode.Created);
        }

        void Move(Context context)
        {
            var source = Store.GetItemByName(context.Request.Url.LocalPath);
            bool isNew = true;

            var destination = Store.GetItemByName(context.Request.Headers["Destination"]);
            if (destination != null)
            {
                if (!GetOverwriteHeader(context.Request))
                    throw new WebDavPreconditionFailedException();
                // else delete destination and set isNew to false
                Store.Delete(context.Request.Headers["Destination"]);
                isNew = false;
            }

            Store.MoveItemHere(context.Request.Url.LocalPath, context.Request.Headers["Destination"]);

            // send correct response
            SendSimpleResponse(context,isNew ? StatusCode.Created : StatusCode.NoContent);
        }

        void Options(Context context)
        {
            List<string> verbsAllowed = new List<string> { "OPTIONS", "TRACE", "GET", "HEAD", "POST", "COPY", "PROPFIND", "LOCK", "UNLOCK" };

            List<string> verbsPublic = new List<string> { "OPTIONS", "GET", "HEAD", "PROPFIND", "PROPPATCH", "MKCOL", "PUT", "DELETE", "COPY", "MOVE", "LOCK", "UNLOCK" };

            foreach (string verb in verbsAllowed)
                context.Response.AppendHeader("Allow", verb);

            foreach (string verb in verbsPublic)
                context.Response.AppendHeader("Public", verb);

            // Sends 200 OK
            SendSimpleResponse(context);
        }

        void Put(Context context)
        {
            var item = Store.GetItemByName(context.Request.Url.LocalPath);

            IWebDavStoreDocument doc;
            if (item != null)
            {
                doc = item as IWebDavStoreDocument;
                if (doc == null)
                    throw new WebDavMethodNotAllowedException();
            }
            else
            {
                doc = Store.CreateDocument(context.Request.Url.LocalPath);
            }

            if (context.Request.ContentLength64 < 0)
                throw new WebDavLengthRequiredException();

            using (Stream stream = doc.OpenWriteStream(false))
            {
                long left = context.Request.ContentLength64;
                byte[] buffer = new byte[4096];
                while (left > 0)
                {
                    int toRead = Convert.ToInt32(Math.Min(left, buffer.Length));
                    int inBuffer = context.Request.InputStream.Read(buffer, 0, toRead);
                    stream.Write(buffer, 0, inBuffer);

                    left -= inBuffer;
                }
            }

            SendSimpleResponse(context,StatusCode.Created);
        }

        void Unlock(Context cnt)
        {
            // Get the item from the collection
            var item = Store.GetItemByName(cnt.Request.Url.LocalPath);
            SendSimpleResponse(cnt,StatusCode.NoContent);
        }

        void PropPatch(Context context)
        {
            // Initiate the XmlNamespaceManager and the XmlNodes
            XmlNamespaceManager manager = null;
            XmlNode propNode = null;

            // try to read the body
            try
            {
                StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                string requestBody = reader.ReadToEnd();

                if (!String.IsNullOrEmpty(requestBody))
                {
                    XmlDocument requestDocument = new XmlDocument();
                    requestDocument.LoadXml(requestBody);

                    if (requestDocument.DocumentElement != null)
                    {
                        if (requestDocument.DocumentElement.LocalName != "propertyupdate")
                        {
                            //FIXME
                            //log.Debug("PROPPATCH method without propertyupdate element in xml document");
                        }

                        manager = new XmlNamespaceManager(requestDocument.NameTable);
                        manager.AddNamespace("D", "DAV:");
                        manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
                        manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
                        manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

                        propNode = requestDocument.DocumentElement.SelectSingleNode("D:set/D:prop", manager);
                    }
                }
            }
            catch (Exception)
            {
                //FIXME
                //log.Warn(ex.Message);
            }

            /***************************************************************************************************
             * Take action
             ***************************************************************************************************/

            // Get the item from the collection
            var item = Store.GetItemByName(context.Request.Url.LocalPath);

            FileInfo fileInfo = new FileInfo(item.ItemPath);

            if (propNode != null && fileInfo.Exists)
            {
                foreach (XmlNode node in propNode.ChildNodes)
                {
                    switch (node.LocalName)
                    {
                        case "Win32CreationTime":
                            fileInfo.CreationTime = Convert.ToDateTime(node.InnerText).ToUniversalTime();
                            break;
                        case "Win32LastAccessTime":
                            fileInfo.LastAccessTime = Convert.ToDateTime(node.InnerText).ToUniversalTime();
                            break;
                        case "Win32LastModifiedTime":
                            fileInfo.LastWriteTime = Convert.ToDateTime(node.InnerText).ToUniversalTime();
                            break;
                        case "Win32FileAttributes":
                            //fileInfo.Attributes = 
                            //fileInfo.Attributes = Convert.ToDateTime(node.InnerText);
                            break;
                    }
                }
            }


            /***************************************************************************************************
             * Create the body for the response
             ***************************************************************************************************/

            // Create the basic response XmlDocument
            XmlDocument responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:multistatus " +
                                       "xmlns:Z=\"urn:schemas-microsoft-com:\" xmlns:D=\"DAV:\">" +
                                       "<D:response></D:response></D:multistatus>";
            responseDoc.LoadXml(responseXml);

            // Select the response node
            XmlNode responseNode = responseDoc.DocumentElement.SelectSingleNode("D:response", manager);

            // Add the elements

            // The href element
            WebDavProperty hrefProperty = new WebDavProperty("href", context.Request.Url.LocalPath);
            responseNode.AppendChild(hrefProperty.ToXmlElement(responseDoc));

            // The propstat element
            WebDavProperty propstatProperty = new WebDavProperty("propstat", "");
            XmlElement propstatElement = propstatProperty.ToXmlElement(responseDoc);

            // The propstat/status element
            WebDavProperty statusProperty = new WebDavProperty("status", "HTTP/1.1 " + context.Response.StatusCode);
            propstatElement.AppendChild(statusProperty.ToXmlElement(responseDoc));

            // The other propstat children
            foreach (WebDavProperty property in from XmlNode child in propNode.ChildNodes
                                                where child.Name.ToLower()
                                                    .Contains("creationtime") || child.Name.ToLower()
                                                        .Contains("fileattributes") || child.Name.ToLower()
                                                            .Contains("lastaccesstime") || child.Name.ToLower()
                                                                .Contains("lastmodifiedtime")
                                                let node = propNode.SelectSingleNode(child.Name, manager)
                                                select node != null
                                                    ? new WebDavProperty(child.LocalName, "", node.NamespaceURI)
                                                    : new WebDavProperty(child.LocalName, "", ""))
                propstatElement.AppendChild(property.ToXmlElement(responseDoc));

            responseNode.AppendChild(propstatElement);

            /***************************************************************************************************
            * Send the response
            ***************************************************************************************************/

            // convert the StringBuilder
            string resp = responseDoc.InnerXml;
            byte[] responseBytes = Encoding.UTF8.GetBytes(resp);


            // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
            context.Response.StatusCode = (int)WebDavStatusCode.MultiStatus;
            //context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int)WebDavStatusCode.MultiStatus);

            // set the headers of the response
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.ContentType = "text/xml";

            // the body
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

            context.Response.Close();
        }

        void PropFind(Context context)
        {
            bool isPropname = false;
            int depth = GetDepthHeader(context.Request);
            var _requestUri = context.Request.Url.LocalPath;

            var _webDavStoreItems = GetWebDavStoreItems(Store.GetItemByName(_requestUri), depth);

            var properties = new List<WebDavProperty>
            {
                new WebDavProperty("creationdate"),
                new WebDavProperty("displayname"),
                new WebDavProperty("getcontentlength"),
                new WebDavProperty("getcontenttype"),
                new WebDavProperty("getetag"),
                new WebDavProperty("getlastmodified"),
                new WebDavProperty("resourcetype"),
                new WebDavProperty("supportedlock"),
                new WebDavProperty("ishidden")
            };

            XmlDocument requestDoc;
            try
            {
                StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                string requestBody = reader.ReadToEnd();
                reader.Close();

                if (!string.IsNullOrEmpty(requestBody))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(requestBody);
                    requestDoc = xmlDocument;
                }
            }
            catch (Exception)
            {
                //_log.Warn("XmlDocument has not been read correctly");
            }

            requestDoc = new XmlDocument();

            // See what is requested
            var _requestedProperties = new List<WebDavProperty>();

            if (requestDoc.DocumentElement != null)
            {
                if (requestDoc.DocumentElement.LocalName == "propfind")
                {
                    XmlNode n = requestDoc.DocumentElement.FirstChild;
                    if (n != null)
                    {
                        switch (n.LocalName)
                        {
                            case "allprop":
                                _requestedProperties = properties;
                                break;
                            case "propname":
                                isPropname = true;
                                _requestedProperties = properties;
                                break;
                            case "prop":
                                foreach (XmlNode child in n.ChildNodes)
                                    _requestedProperties.Add(new WebDavProperty(child.LocalName, "", child.NamespaceURI));
                                break;
                            default:
                                _requestedProperties.Add(new WebDavProperty(n.LocalName, "", n.NamespaceURI));
                                break;
                        }
                    }
                }
            }
            else
            {
                _requestedProperties = properties;
            }

            XmlDocument responseDoc = ResponseDocument(context, isPropname, _webDavStoreItems, _requestedProperties);

            // convert the XmlDocument
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseDoc.InnerXml);

            // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
            context.Response.StatusCode = (int)WebDavStatusCode.MultiStatus;
            //context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int)WebDavStatusCode.MultiStatus);

            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.ContentType = "text/xml";
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

            context.Response.Close();
        }

        public void Lock(Context context)
        {
            int depth = GetDepthHeader(context.Request);
            string timeout = GetTimeoutHeader(context.Request);

            // Initiate the XmlNamespaceManager and the XmlNodes
            XmlNamespaceManager manager = null;
            XmlNode lockscopeNode = null, locktypeNode = null, ownerNode = null;

            // try to read the body
            try
            {
                StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                string requestBody = reader.ReadToEnd();

                if (!requestBody.Equals("") && requestBody.Length != 0)
                {
                    XmlDocument requestDocument = new XmlDocument();
                    requestDocument.LoadXml(requestBody);

                    if (requestDocument.DocumentElement != null && requestDocument.DocumentElement.LocalName != "prop" &&
                        requestDocument.DocumentElement.LocalName != "lockinfo")
                    {
                        //FIXME
                        //log.Debug("LOCK method without prop or lockinfo element in xml document");
                    }

                    manager = new XmlNamespaceManager(requestDocument.NameTable);
                    manager.AddNamespace("D", "DAV:");
                    manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
                    manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
                    manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

                    // Get the lockscope, locktype and owner as XmlNodes from the XML document
                    lockscopeNode = requestDocument.DocumentElement.SelectSingleNode("D:lockscope", manager);
                    locktypeNode = requestDocument.DocumentElement.SelectSingleNode("D:locktype", manager);
                    ownerNode = requestDocument.DocumentElement.SelectSingleNode("D:owner", manager);
                }
                else
                {
                    throw new WebDavPreconditionFailedException();
                }
            }
            catch (Exception)
            {
                //FIXME
                //log.Warn(ex.Message);
                throw;
            }

            bool isNew = false;

            try
            {
                // Get the item from the collection
                IWebDavStoreItem item = Store.GetItemByName(context.Request.Url.LocalPath);
            }
            catch (Exception)
            {
                Store.CreateDocument(context.Request.Url.LocalPath);
                isNew = true;
            }

            // Create the basic response XmlDocument
            XmlDocument responseDoc = new XmlDocument();
            string responseXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:prop xmlns:D=\"DAV:\"><D:lockdiscovery><D:activelock/></D:lockdiscovery></D:prop>";
            responseDoc.LoadXml(responseXml);

            // Select the activelock XmlNode
            XmlNode activelock = responseDoc.DocumentElement.SelectSingleNode("D:lockdiscovery/D:activelock", manager);

            // Import the given nodes
            activelock.AppendChild(responseDoc.ImportNode(lockscopeNode, true));
            activelock.AppendChild(responseDoc.ImportNode(locktypeNode, true));
            activelock.AppendChild(responseDoc.ImportNode(ownerNode, true));

            // Add the additional elements, e.g. the header elements

            // The timeout element
            WebDavProperty timeoutProperty = new WebDavProperty("timeout", timeout);
            activelock.AppendChild(timeoutProperty.ToXmlElement(responseDoc));

            // The depth element
            WebDavProperty depthProperty = new WebDavProperty("depth", (depth == 0 ? "0" : "Infinity"));
            activelock.AppendChild(depthProperty.ToXmlElement(responseDoc));

            // The locktoken element
            WebDavProperty locktokenProperty = new WebDavProperty("locktoken", "");
            XmlElement locktokenElement = locktokenProperty.ToXmlElement(responseDoc);
            WebDavProperty hrefProperty = new WebDavProperty("href", "opaquelocktoken:e71d4fae-5dec-22df-fea5-00a0c93bd5eb1");
            locktokenElement.AppendChild(hrefProperty.ToXmlElement(responseDoc));
            activelock.AppendChild(locktokenElement);

            /***************************************************************************************************
             * Send the response
             ***************************************************************************************************/

            // convert the StringBuilder
            string resp = responseDoc.InnerXml;
            byte[] responseBytes = Encoding.UTF8.GetBytes(resp);

            if (isNew)
            {
                // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
                context.Response.StatusCode = (int)HttpStatusCode.Created;
                //context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.Created);
            }
            else
            {
                // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                //context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.OK);
            }


            // set the headers of the response
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.ContentType = "text/xml";

            // the body
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

            context.Response.Close();
        }
    }
}
