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

using System;

namespace NetFluid
{
    /// <summary>
    /// Implements methods for system logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Get or set the current log level.Messages under this level are ignored and not logged
        /// </summary>
        LogLevel LogLevel { get; set; }

        /// <summary>
        /// Get or set path of the logs
        /// </summary>
        string LogPath { get; set; }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="lvl">Log level of the message</param>
        /// <param name="msg">The message be be logged</param>
        void Log(LogLevel lvl, string msg);

        /// <summary>
        /// Log an exception
        /// </summary>
        /// <param name="lvl">Log level of the exception</param>
        /// <param name="msg">A presentation message</param>
        /// <param name="ex">The exception to be logged</param>
        void Log(LogLevel lvl, string msg, Exception ex);
    }
}