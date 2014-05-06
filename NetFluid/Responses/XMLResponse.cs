namespace NetFluid
{
    public class XMLResponse:IResponse
    {
        readonly object obj;

        public XMLResponse(object obj)
        {
            this.obj = obj;
        }

        public void SetHeaders(Context cnt)
        {
            cnt.Response.ContentType = "application/xml";
        }

        public void SendResponse(Context cnt)
        {
            obj.ToXML(cnt.OutputStream);
        }
    }
}
