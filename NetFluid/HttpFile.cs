// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System.IO;

namespace NetFluid
{
    /// <summary>
    /// Multipart form data file uploaded by the client
    /// </summary>
    public sealed class HttpFile
    {
        /// <summary>
        ///     Mimetype of recieved file
        /// </summary>
        public string ContentType;

        /// <summary>
        ///     Extension of recieved file.Empty if missing
        /// </summary>
        public string Extension;

        /// <summary>
        ///     Original filename
        /// </summary>
        public string FileName;

        /// <summary>
        ///     Original filename without extension
        /// </summary>
        public string FileNameWithoutExtesion;

        /// <summary>
        ///     Post variable name in request
        /// </summary>
        public string Name;

        /// <summary>
        ///     Temp path on server
        /// </summary>
        public string TempFile { get; internal set; }

        public void SaveAs(string path, bool rewrite = true)
        {
            File.Copy(TempFile, path, rewrite);
        }

        public Stream Open()
        {
            return File.OpenRead(TempFile);
        }

        public string ReadAllText()
        {
            return File.ReadAllText(TempFile);
        }

        public byte[] ReadAllBytes()
        {
            return File.ReadAllBytes(TempFile);
        }
    }
}