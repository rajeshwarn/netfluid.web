using System.Collections.Generic;

namespace NetFluid
{
    public class StringResponse : IResponse
    {
        private byte[] bytes;

        public StringResponse(string p)
        {
            String = p;
        }

        public string String { get; set; }

        public void SetHeaders(Context cnt)
        {
        }

        public void SendResponse(Context cnt)
        {
            var k = cnt.Request.ContentEncoding.GetBytes(String);
            cnt.OutputStream.Write(k,0,k.Length);
            cnt.OutputStream.Flush();
        }
    }
}