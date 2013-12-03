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

using System.Net;
using System.Net.NetworkInformation;

namespace NetFluid.HTTP
{
    /// <summary>
    /// Return important data about the connected network
    /// </summary>
    public class Network
    {
        /// <summary>
        /// All available physical network interfaces
        /// </summary>
        public static NetworkInterface[] Interfaces
        {
            get { return NetworkInterface.GetAllNetworkInterfaces(); }
        }

        /// <summary>
        /// True if the machine is connected to the network
        /// </summary>
        public static bool Connected
        {
            get { return NetworkInterface.GetIsNetworkAvailable(); }
        }

        /// <summary>
        /// Return the loopback physical inetrface
        /// </summary>
        public static NetworkInterface Loopback
        {
            get { return Interfaces[NetworkInterface.LoopbackInterfaceIndex]; }
        }

        /// <summary>
        /// Return all ip address of the current machine
        /// </summary>
        public static IPAddress[] Addresses
        {
            get { return System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList; }
        }
    }
}