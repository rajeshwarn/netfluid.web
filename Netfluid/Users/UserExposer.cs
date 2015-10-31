using Netfluid;
using System;
using System.Linq;

namespace Netfluid.Users
{
    class UserExposer<T> where T:User
	{
        NetfluidHost Host;
        UserManager<T> UserManager;

        public UserExposer(NetfluidHost host,UserManager<T> manager)
        {
            Host = host;
            UserManager = manager;

            host.Filters.Add(Route.New(new Func<Context,dynamic>(WalledGarden)));

            host.Routes["GET", "/users/signin"] = new Route(new Func<IResponse>(SignIn));
            host.Routes["POST", "/users/signin"] = new Route(new Func<Context,string,string,IResponse>(SignedIn));

            host.Routes["/users/signout"] = new Route(new Func<Context,IResponse>(SignOut));

            host.Routes["GET", "/users/signup"] = new Route(new Func<IResponse>(SignUp));
            host.Routes["POST", "/users/signup"] = new Route(new Func<Context,string,IResponse>(SignedUp));
        }
        
        dynamic WalledGarden(Context context)
        {
            if (!UserManager.WalledGarden) return false;

            if (context.Session<T>("user") == null && context.Request.Url.LocalPath != "/users/signin")
            {
                if (Host.PublicFolders.Any(y => y.Map(context)))
                    return false;

                if(context.Request.HttpMethod=="GET" && !context.Request.Url.LocalPath.Contains('.'))
                    context.Session("redirect", context.Request.Url.LocalPath);

                return new MustacheTemplate("./views/user/sign_in.html");
            }

            return false;
        }

        IResponse SignIn()
        {
            return new MustacheTemplate("./views/user/sign_in.html");
        }

        IResponse SignedIn(Context context, string user, string pass)
		{
            if (string.IsNullOrWhiteSpace(user))
                return new MustacheTemplate("./views/user/sign_in.html", "Username required");

            if (string.IsNullOrWhiteSpace(pass))
                return new MustacheTemplate("./views/user/sign_in.html", "Password required");

            var u = UserManager.SignIn(user, pass);

            if (u == null)
                return new MustacheTemplate("./views/user/sign_in.html", "Invalid user name or password.");

            context.Session("user", u);

            var redirect = context.Session<string>("redirect") ?? "/";

            return new RedirectResponse(redirect);
		}
		
        IResponse SignOut(Context cnt)
        {
            cnt.SessionDelete("user");
            return new RedirectResponse("/");
        }

        IResponse SignUp()
        {
            return new MustacheTemplate("./views/user/sign_up.html");
        }

        IResponse SignedUp(Context cnt,string password)
        {
            var user = cnt.Values.Parse<T>();

            if (string.IsNullOrWhiteSpace(user.UserName))
                return new MustacheTemplate("./views/user/sign_in.html", "Username is mandatory");

            if (string.IsNullOrWhiteSpace(password))
                return new MustacheTemplate("./views/user/sign_in.html", "Password is mandatory");

            if (UserManager.Exists(user))
                return new MustacheTemplate("./views/user/sign_in.html", "Username already taken");

            if(!UserManager.Add(user, password))
                return new MustacheTemplate("./views/user/sign_in.html", "Something went wrong");

            return new RedirectResponse("/");
        }
    }
}