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

namespace NetFluid.DNS.Records
{
        [Serializable]
    public class RecordTXT : Record
    {
        public string TXT;

        public static RecordTXT Parse(string s)
        {
            return new RecordTXT { TXT = s };
        }

        public override string ToString()
        {
            return string.Format("\"{0}\"", TXT);
        }
    }
}