namespace NetFluid
{
    /// <summary>
    /// Permanent redirect for clients
    /// </summary>
    public class MovedPermanentlyResponse:IResponse
    {
        /// <summary>
        /// Absolute or relative URI
        /// </summary>
        public string Destination;
        
        public void SetHeaders(Context cnt)
        {
            cnt.Response.MovedPermanently(Destination);
        }

        public void SendResponse(Context cnt)
        {
        }
    }
}
