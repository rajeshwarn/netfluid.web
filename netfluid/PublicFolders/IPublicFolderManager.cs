using System.Collections.Generic;

namespace NetFluid
{
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
        /// Return all managed files
        /// </summary>
        IEnumerable<string> Files { get; }
    }
}