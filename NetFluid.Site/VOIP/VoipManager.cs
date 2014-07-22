using Twitterizer;

namespace NetFluid.Site.VOIP
{
    [Route("voip")]
    public class VoipManager : FluidPage
    {
        public Context Context { get; set; }

        [Route("chat")]
        public IResponse Main()
        {
            string id = Session("voipUser") == null
                ? Security.Random().ToString("X")
                : Session<VoipUser>("voipUser").PeerId;

            return new FluidTemplate("./VOIP/voip.html", id);
        }

        [Route("/twlogin")]
        public IResponse TwitterLogin(string oauth_token, string oauth_verifier)
        {
            string oauth_consumer_key = "rSjAYm8f4C0pdb0l09Fa6BEMI";

            string oauth_consumer_secret = "d73WzSExzaiRcAdthzH7fyttqK191w5SEPjqivzygBzWGNSHCu";


            if (oauth_token == null)
            {
                OAuthTokenResponse reqToken = OAuthUtility.GetRequestToken(
                    oauth_consumer_key,
                    oauth_consumer_secret,
                    "http://www.netfluid.org/voip/chat");


                return new RedirectResponse((string.Format(OAuthUtility.BuildAuthorizationUri(reqToken.Token, true).ToString(),
                    reqToken.Token)));
            }
            else
            {
                string requestToken = oauth_token;

                string pin = oauth_verifier;


                OAuthTokenResponse tokens = OAuthUtility.GetAccessToken(
                    oauth_consumer_key,
                    oauth_consumer_secret,
                    requestToken,
                    pin);


                var accesstoken = new OAuthTokens

                {
                    AccessToken = tokens.Token,
                    AccessTokenSecret = tokens.TokenSecret,
                    ConsumerKey = oauth_consumer_key,
                    ConsumerSecret = oauth_consumer_secret
                };


                TwitterResponse<TwitterStatus> response = TwitterStatus.Update(
                    accesstoken,
                    "NetFluid - Twitter login test");


                if (response.Result == RequestResult.Success)
                {
                    return new StringResponse("Did it yaar");
                }
                else
                {
                    return new StringResponse("Try some other time");
                }
            }
            return new StringResponse("OK");
        }

        public void OnLoad()
        {
        }
    }
}