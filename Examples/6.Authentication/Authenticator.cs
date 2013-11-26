using NetFluid;
using System;

namespace _6.Authentication
{
    class Authenticator : FluidPage
    {
        // basic http authentication
        // http://en.wikipedia.org/wiki/Basic_access_authentication
        [Route("/basic")]
        public string Basic()
        {
            string user, password;

            if (NetFluid.Authentication.Basic(Context,"Write user equals to password to log in",out user,out password))
            {
                // Client has sent username and password so we can "check" them
                if (user==password)
                    return "Hi there " + user + "!";

                return "Bad user name or password";
            }
            // The context ends with credential require
            return "Please provide your credentials";
        }

        // digest http authentication
        //http://en.wikipedia.org/wiki/Digest_access_authentication
        [Route("/digest")]
        public string Digest()
        {
            string user, password;

            if (NetFluid.Authentication.Digest(Context, "Write user equals to password to log in", out user, out password))
            {
                // Client has sent username and password so we can "check" them
                if (user == password)
                    return "Hi there " + user + "!";

                return "Bad user name or password";
            }
            // The context ends with credential request
            return "Please provide your credentials";
        }
    }
}
