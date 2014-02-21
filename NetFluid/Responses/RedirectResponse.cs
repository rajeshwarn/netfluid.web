namespace NetFluid.Responses
{
    public class RedirectResponse:IResponse
    {
        public string Uri { get; set; }

        public RedirectResponse(string to)
        {
            Uri = to;
        }
        public void SendResponse(Context cnt)
        {
            cnt.Response.Redirect(Uri);
            cnt.Close();
        }
    }
}
