namespace NetFluid
{
    public class RedirectResponse:IResponse
    {
        public string Uri { get; set; }

        public RedirectResponse(string to)
        {
            Uri = to;
        }

        public void SetHeaders(Context cnt)
        {
            cnt.Response.Redirect(Uri);
        }

        public void SendResponse(Context cnt)
        {
        }
    }
}
