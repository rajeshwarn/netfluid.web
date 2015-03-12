using System.Collections.Generic;

namespace NetFluid
{
    public class StringResponse : IResponse
    {
        public StringResponse(string p)
        {
            String = p;
        }

        public StringResponse(object p)
        {
            this.String = p.ToString();
        }

        public string String { get; set; }

        public void SetHeaders(Context cnt)
        {
        }

        public void SendResponse(Context cnt)
        {
            var k = cnt.Request.ContentEncoding.GetBytes(String);
            cnt.Response.OutputStream.Write(k,0,k.Length);
            cnt.Response.OutputStream.Flush();
        }

        public void Dispose()
        {
           
        }
    }
}