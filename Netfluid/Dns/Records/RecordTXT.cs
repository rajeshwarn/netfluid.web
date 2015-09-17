#region Rfc info

/*
3.3.14. TXT RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   TXT-DATA                    /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

TXT-DATA        One or more <character-string>s.

TXT RRs are used to hold descriptive text.  The semantics of the text
depends on the domain where it is found.
 * 
*/

#endregion

using System;

namespace Netfluid.Dns.Records
{
    /// <summary>
    /// DNS record TXT
    /// </summary>
        [Serializable]
    public class RecordTXT : Record
    {
        public string Text;

        public static RecordTXT Parse(string s)
        {
            return new RecordTXT { Text = s };
        }

        public override string ToString()
        {
            return string.Format("\"{0}\"", Text);
        }
    }
}