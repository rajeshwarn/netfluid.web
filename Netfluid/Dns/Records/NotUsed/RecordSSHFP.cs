/*

 */

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record SSHPF (work in progress)
    /// </summary>
[Serializable]
    public class RecordSSHFP : Record
    {
        public byte[] RDATA;

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}