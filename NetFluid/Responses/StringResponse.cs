using System.Collections.Generic;

namespace NetFluid
{
    public class StringResponse : IResponse
    {
        private readonly bool flusheveryline;
        private readonly IEnumerable<string> strings;

        public StringResponse(string str, bool flusheveryline = false)
        {
            strings = new List<string> {str};
            this.flusheveryline = flusheveryline;
        }

        public StringResponse(IEnumerable<string> strings, bool flusheveryline = false)
        {
            this.strings = strings;
            this.flusheveryline = flusheveryline;
        }

        public void SetHeaders(Context cnt)
        {
        }

        public void SendResponse(Context cnt)
        {
            foreach (var s in strings)
            {
                cnt.Writer.Write(s);
                if (flusheveryline)
                    cnt.Writer.Flush();
            }
        }
    }
}