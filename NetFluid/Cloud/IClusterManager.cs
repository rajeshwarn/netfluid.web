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


namespace NetFluid.Cloud
{
    /// <summary>
    /// Handle reverse proxy to other machines of the cluster
    /// </summary>
    public interface IClusterManager
    {
        /// <summary>
        /// Instance a reverse proxy from the virtual host to a real endpoint.All context with this host will be fowarded. 
        /// </summary>
        /// <param name="host">virtual host to foward (ex: users.netfluid.org)</param>
        /// <param name="remote">real endpoint ("http://" || "https://"){0,1}( <ip>|<hostname>)(:<port>){0,1}</param>
        void AddFowarding(string host, string remote);

        /// <summary>
        /// Remove reverse proxy for the specified virtual host
        /// </summary>
        /// <param name="host">virtual host to be removed</param>
        void RemoveFowarding(string host);

        /// <summary>
        /// If the context host is a reversed proxy virtual host foward it
        /// </summary>
        /// <param name="context">context to check and foward</param>
        /// <returns></returns>
        bool Handle(Context context);

        /// <summary>
        /// Foward this context to specified remote, instancing a reverse proxy just for this context
        /// </summary>
        /// <param name="context">context to foward</param>
        /// <param name="remote">real endpoint ("http://" || "https://"){0,1}( <ip>|<hostname>)(:<port>){0,1}</param>
        void Foward(Context context, string remote);
    }
}