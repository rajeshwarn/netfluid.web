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
        void Add(string uriPath, string realPath);

        bool TryGetFile(Context cnt);

    }
}