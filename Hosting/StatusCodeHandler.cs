namespace Netfluid
{
    public class StatusCodeHandler:Route
    {
        public StatusCode StatusCode { get; set; }

        public override dynamic Handle(Context cnt)
        {
            if (cnt.Response.StatusCode == (int)StatusCode || StatusCode == StatusCode.AnyError || (cnt.Response.StatusCode >= 400 && cnt.Response.StatusCode <500 && StatusCode == StatusCode.AnyClientError) || (cnt.Response.StatusCode >= 500 && StatusCode == StatusCode.AnyServerError))
            {
                return base.Handle(cnt);
            }
            return false;
        }
    }
}
