using System.Collections.Generic;
using System.IO;

namespace NetFluid
{
    /// <summary>
    /// Interface to override Engine public folder managing
    /// </summary>
    public interface IPublicFolderManager
    {
        /// <summary>
        /// If the requested URI is mapped by one of the pubblic folder, serve and close the context
        /// </summary>
        /// <param name="cnt"></param>
        void Serve(Context cnt);

        /// <summary>
        /// Add  a file-donwloadable folder
        /// </summary>
        /// <param name="folder"></param>
        void Add(PublicFolder folder);

        /// <summary>
        /// Add a file-downloadable folder
        /// </summary>
        /// <param name="id">Friendly name for the folder</param>
        /// <param name="uriPath">URI to map</param>
        /// <param name="realPath">Physical path</param>
        void Add(string id, string uriPath, string realPath);

        /// <summary>
        /// Remove the public folder
        /// </summary>
        /// <param name="id"></param>
        void Remove(string id);

        /// <summary>
        /// Return all managed directories
        /// </summary>
        IEnumerable<string> Directories { get; }

        /// <summary>
        /// Return all managed files physical path
        /// </summary>
        IEnumerable<string> Files { get; }

        /// <summary>
        /// Returns all mapped URI
        /// </summary>
        IEnumerable<string> URIs { get; }

        /// <summary>
        /// Return physical path from URI
        /// </summary>
        /// <param name="uri">URI ofthe resource</param>
        /// <returns>physical or simulated (ex:database id) path to the file</returns>
        string ToPath(string uri);

        /// <summary>
        /// Return the URI mapped by the physical or simulated (ex:database id) path
        /// </summary>
        /// <param name="path">physical path</param>
        /// <returns>mapped uri</returns>
        string ToUri(string path);

        /// <summary>
        /// Open the public file from the URI
        /// </summary>
        /// <param name="uri">uri path to the file (used to copy setting from one manager to another)</param>
        /// <returns>real o simulated file stream</returns>
        Stream OpenFile(string uri);
    }
}