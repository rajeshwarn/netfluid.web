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

using System.Collections.Generic;
using System.Net;

namespace NetFluid
{
    /// <summary>
    /// Implements methods to create and manage system http interfaces
    /// </summary>
    public interface IWebInterfaceManager : IEnumerable<IWebInterface>
    {
        /// <summary>
        ///     Add an http interface on ip 127.0.0.1 and spefied port
        /// </summary>
        /// <param name="port">Port on wich listening.80 by default</param>
        void AddLoopBack(int port = 80);

        /// <summary>
        ///     Add an http interface on any available ip excluding loopback on specified port.
        /// </summary>
        /// <param name="port">Port on wich listening.80 by default</param>
        void AddAllAddresses(int port = 80);

        /// <summary>
        ///     Add an http interface on specified ip and port
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        void AddInterface(IPAddress ip, int port);

        /// <summary>
        ///     Add an HTTPS interface on specified ip and port
        /// </summary>
        /// <param name="ip">
        ///     Ip on wich listening/param>
        ///     <param name="port">Port on wich listening</param>
        ///     <param name="certificate">Path of pfx certificate</param>
        void AddInterface(IPAddress ip, int port, string certificate);

        /// <summary>
        ///     Add an http interface on specified ip and port
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        void AddInterface(string ip, int port);

        /// <summary>
        ///     Add an HTTPS interface on specified ip and port
        /// </summary>
        /// <param name="ip">
        ///     Ip on wich listening/param>
        ///     <param name="port">Port on wich listening</param>
        ///     <param name="certificate">Path of pfx certificate</param>
        void AddInterface(string ip, int port, string certificate);

        /// <summary>
        ///     Start to accept and serve clients
        /// </summary>
        void Start();
    }
}