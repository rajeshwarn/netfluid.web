using System.Collections.Generic;

namespace NetFluid
{
    public class StringResponse : IResponse
    {
        private IEnumerable<string> strings;
        private bool flusheveryline;

        public StringResponse(string str, bool flusheveryline = false)
        {
            this.strings = new List<string> { str };
            this.flusheveryline = flusheveryline;
        }

        public StringResponse(IEnumerable<string> strings, bool flusheveryline = false)
        {
            this.strings = strings;
            this.flusheveryline = flusheveryline;
        }

        public void SendResponse(Context cnt)
        {
            cnt.SendHeaders();
            foreach (string s in this.strings)
            {
                cnt.Writer.Write(s);
                if (this.flusheveryline)
                {
                    cnt.Writer.Flush();
                }
            }
        }
    }
}