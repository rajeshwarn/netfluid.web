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
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetFluid
{
    internal class Logger : ILogger
    {
        public Logger()
        {
            LogPath = "./AppLog.txt";
            LogLevel = LogLevel.All;
        }

        #region ILogger Members

        public LogLevel LogLevel { get; set; }

        public string LogPath { get; set; }

        public void Log(LogLevel lvl, string msg, Exception ex)
        {
            if (LogLevel >= lvl)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;

                do
                {
                    string s = ("\r\n" + DateTime.Now + "\t" + lvl.ToString() + "\t" + msg + "\r\n");
                    if (Engine.DevMode)
                        Console.WriteLine(s);

                    s = ex.ToString().Split('\r', '\n').Aggregate(s,
                                                                  (current, item) =>
                                                                  current + ("\t\t\t\t" + item + "\r\n"));

                    File.AppendAllText(LogPath, s);
                    ex = ex.InnerException;
                } while (ex != null);
            }
        }


        public void Log(LogLevel lvl, string msg)
        {
            if (LogLevel >= lvl)
            {
                string s = DateTime.Now + "\t" + lvl.ToString() + "\t" + msg + "\r\n";
                if (Engine.DevMode)
                {
                    Console.Write(s);
                }

                File.AppendAllText(LogPath, s);
            }
        }

        #endregion
    }
}