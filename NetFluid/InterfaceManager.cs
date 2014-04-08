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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetFluid.HTTP;
using System;

namespace NetFluid
{
    internal class InterfaceManager : List<IWebInterface>, IWebInterfaceManager
    {
        #region IWebInterfaceManager Members

        public void Start()
        {
            Engine.Logger.Log(LogLevel.Debug, "Starting web interfaces");
            foreach (var item in this)
            {
                item.Start();
            }
        }

        public void AddLoopBack(int port = 80)
        {
            AddInterface(IPAddress.Loopback, port);
        }

        public void AddAllAddresses(int port = 80)
        {
            Engine.Logger.Log(LogLevel.Debug, "Starting web interfaces on every ip on port " + port);
            foreach (var item in Network.Addresses)
            {
                //Console.WriteLine(item + ":" + port);
                AddInterface(item, port);
            }
        }

        public void AddInterface(IPAddress ip, int port)
        {
            try
            {
                Engine.Logger.Log(LogLevel.Debug, "Adding http interface on " + ip + ":" + port);
                Add(new WebInterface(ip, port));
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Warning, "Failed to add http interface on " + ip + ":" + port, ex);
            }
        }

        public void AddInterface(IPAddress ip, int port, string certificate)
        {
            try
            {
                Engine.Logger.Log(LogLevel.Debug, "Adding https interface on " + ip + ":" + port);
                Add(new WebInterface(ip, port, certificate));
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Warning, "Failed to add https interface on " + ip + ":" + port, ex);
            }

        }

        public void AddInterface(string ip, int port)
        {
            try
            {
                Engine.Logger.Log(LogLevel.Debug, "Adding http interface on " + ip + ":" + port);
                Add(new WebInterface(IPAddress.Parse(ip), port));
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Warning, "Failed to add http interface on " + ip + ":" + port, ex);
            }

        }

        public void AddInterface(string ip, int port, string certificate)
        {
            try
            {
                Engine.Logger.Log(LogLevel.Debug, "Adding https interface on " + ip + ":" + port);
                Add(new WebInterface(IPAddress.Parse(ip), port, certificate));
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Warning, "Failed to add https interface on " + ip + ":" + port, ex);
            }
        }

        #endregion
    }
}