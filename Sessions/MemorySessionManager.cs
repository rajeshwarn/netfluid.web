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
using System.Linq;
using System.Runtime.Caching;

namespace Netfluid.Sessions
{
    internal class MemorySessionManager : ISessionManager
    {
        private readonly MemoryCache sessions;

        public MemorySessionManager()
        {
            sessions = MemoryCache.Default;
            SessionDuration = 3600;
        }

        #region ISessionManager Members

        public int SessionDuration { get; set; }

        public void Set(string sessionId, string name, object obj)
        {
            if (obj == null)
            {
                sessions.Remove(sessionId + "." + name);
                return;
            }
            sessions.Set(sessionId + "." + name, obj, DateTimeOffset.Now + TimeSpan.FromSeconds(SessionDuration));
        }

        public object Get(string sessionId, string name)
        {
            return sessions.Get(sessionId + "." + name);
        }

        public void Remove(string sessionId, string name)
        {
            sessions.Remove(sessionId + "." + name);
        }

        public void Destroy(string sessionId)
        {
            sessions.ForEach(x=>
            {
                if (x.Key.StartsWith(sessionId + "."))
                    sessions.Remove(x.Key);
            });
        }

        public bool HasItems(string sessionId)
        {
            var id = sessionId + ".";
            return sessions.Any(x => x.Key.StartsWith(id));
        }

        #endregion
    }
}