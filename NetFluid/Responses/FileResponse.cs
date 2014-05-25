﻿// ********************************************************************************************************
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

using System;
using System.IO;

namespace NetFluid
{
    /// <summary>
    /// Send a file to the client (http://stackoverflow.com/questions/1012437/uses-of-content-disposition-in-an-http-response-header)
    /// </summary>
    public class FileResponse : IResponse
    {
        /// <summary>
        /// Send this file to client faking filename and mimetype
        /// </summary>
        /// <param name="filepath">original file path</param>
        /// <param name="filename">overrided file name</param>
        /// <param name="mimetype">overrided mime type</param>
        public FileResponse(string filepath, string filename, string mimetype)
        {
            Path = System.IO.Path.GetFullPath(filepath);
            FileName = filename;
            MimeType = mimetype;
        }

        /// <summary>
        ///  Send this file to client faking filename
        /// </summary>
        /// <param name="filepath">original file path</param>
        /// <param name="filename">overrided file name</param>
        public FileResponse(string filepath, string filename)
        {
            Path = System.IO.Path.GetFullPath(filepath);
            FileName = filename;
            MimeType = MimeTypes.GetType(Path);
        }

        /// <summary>
        /// Send this file to client.
        /// </summary>
        /// <param name="filepath"></param>
        public FileResponse(string filepath)
        {
            Path = System.IO.Path.GetFullPath(filepath);
            FileName = System.IO.Path.GetFileName(Path);
            MimeType = MimeTypes.GetType(Path);
        }

        /// <summary>
        /// Get or set file name exposed to the client
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File to send physical path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///  Get or set file mime type exposed to the client
        /// </summary>
        public string MimeType { get; set; }

        public event Action<string> OnDownloadCompleted; 

        #region IResponse Members

        /// <summary>
        /// Set headers
        /// </summary>
        /// <param name="cnt"></param>
        public void SetHeaders(Context cnt)
        {
            cnt.Response.Headers["Content-Disposition"] = "attachment; filename=\"" + FileName + "\"";
            cnt.Response.ContentType = MimeType;
        }

        /// <summary>
        /// Send the file to client
        /// </summary>
        /// <param name="cnt">client context</param>
        public void SendResponse(Context cnt)
        {
            var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            #region TO BE IMPLEMENTED
            /*if (cnt.Request.Headers.Contains("Range") && cnt.Request.Headers["Range"].StartsWith("bytes="))
            {
                var r = cnt.Request.Headers["Range"].Substring("bytes=".Length);
                var index = r.IndexOf('/');

                if (index >= 0) r = r.Substring(0, index);

                var parts = r.Split(new[]{'-'}, StringSplitOptions.None);

                var from = int.Parse(parts[0] == string.Empty ? "0" : parts[0]);
                var to = int.Parse(parts[1] == string.Empty ? "0" : parts[1]);

                fs.Seek(from, SeekOrigin.Begin);

                if (to!=0)
                {
                    fs.CopyTo(cnt.);
                }
            }*/
            #endregion

            fs.CopyTo(cnt.OutputStream);
            fs.Close();

            if (OnDownloadCompleted != null)
                OnDownloadCompleted(Path);
        }

        #endregion
    }
}