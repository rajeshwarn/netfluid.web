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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetFluid
{
    internal class Logger : ILogger
    {
        FileStream fs;
        StreamWriter writer;
        BlockingCollection<string> outQueue;

        public Logger()
        {
            LogPath = "./AppLog.txt";
            outQueue = new BlockingCollection<string>();
            fs = new FileStream(LogPath, FileMode.OpenOrCreate);
            writer = new StreamWriter(fs);

            Task.Factory.StartNew(() =>
            {
                while (true) 
                {
                    writer.WriteLine(outQueue.Take());
                    writer.Flush();
                }
            });
        }

        #region ILogger Members

        public string LogPath { get; set; }

        public void Log(string msg, Exception ex)
        {
            if (ex is TargetInvocationException)
                ex = ex.InnerException;

            string s = ("\r\n" + DateTime.Now + "\t" + msg + "\r\n");
            do
            {
                if (Engine.DevMode)
                    Console.WriteLine(s);

                s += string.Join("\r\n",ex.ToString().Split('\r', '\n').Select(item=>"\t\t\t" + item.Trim()));

                ex = ex.InnerException;
            } while (ex != null);

            outQueue.Add(s);
        }


        public void Log(string msg)
        {
            string s = DateTime.Now + "\t" + msg;
            if (Engine.DevMode)
                Console.WriteLine(s);

            outQueue.Add(s);
        }

        #endregion
    }
}