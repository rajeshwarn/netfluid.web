using Netfluid;
using System.Linq;

namespace Netfluid.Users
{
    class UserExposer
	{
        [Filter]
        public dynamic WalledGarden(Context context)
        {
            if (!UserManager.WalledGarden) return false;

            if (context.Session<User>("user") == null && context.Request.Url.LocalPath != "/users/signin")
            {
                if (Program.InternalHost.PublicFolders.Any(y => y.Map(context)))
                    return false;

                if(context.Request.HttpMethod=="GET" && !context.Request.Url.LocalPath.Contains('.'))
                    context.Session("redirect", context.Request.Url.LocalPath);

                return new MustacheTemplate("./views/user/sign_in.html");
            }

            return false;
        }

        [Route("/users/signin", "GET")]
        public IResponse SignIn()
        {
            return new MustacheTemplate("./views/user/sign_in.html");
        }

        [Route("/users/signin","POST")]
        public IResponse SignIn(Context context, string user, string domain, string pass)
		{
            if (string.IsNullOrWhiteSpace(user))
                return new MustacheTemplate("./views/user/sign_in.html", "Username required");

            if (string.IsNullOrWhiteSpace(pass))
                return new MustacheTemplate("./views/user/sign_in.html", "Password required");

            var u = UserManager.SignIn(string.IsNullOrEmpty(domain) ? user : user + "@" + domain, pass);

            if (u == null)
                return new MustacheTemplate("./views/user/sign_in.html", "Invalid user name or password.");

            context.Session("user", u);

            var redirect = context.Session<string>("redirect") ?? "/";

            return new RedirectResponse(redirect);
		}
		
        [Route("/users/signout")]
        public IResponse SignOut(Context cnt)
        {
            cnt.SessionDelete("user");
            return new RedirectResponse("/");
        }

        public IResponse SignedUp(Context cnt, string displayName, string domain, string email, string username, string password )
        {
            displayName = displayName.HTMLEncode();
            domain = domain.HTMLEncode();
            email = email.HTMLEncode();


            if (string.IsNullOrWhiteSpace(username))
                return new MustacheTemplate("./views/user/sign_in.html", "Username is mandatory");

            if (string.IsNullOrWhiteSpace(password))
                return new MustacheTemplate("./views/user/sign_in.html", "Password is mandatory");

            username = username.HTMLEncode();

            var user = new User
            {
                DisplayName = displayName,
                Domain = domain,
                Email = email,
                UserName = username
            };

            if (UserManager.Exists(user))
                return new MustacheTemplate("./views/user/sign_in.html", "Username already taken");

            if(!UserManager.Add(user, password, UserManager.System))
                return new MustacheTemplate("./views/user/sign_in.html", "Something went wrong");

            return new RedirectResponse("/");
        }
    }
}