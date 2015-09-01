using System;
using System.Linq;

namespace Netfluid.Users
{
	public class HtmlUserExposer
	{
        UserManager manager;

        public Func<Context, IResponse> SignInForm { get; set; }
        public Func<Context, string, IResponse> SignInError { get; set; }
        public Func<Context, IResponse> SignInOK { get; set; }

        public Func<Context, IResponse> SignOutOK { get; set; }

        public Func<Context, IResponse> SignUpForm { get; set; }
        public Func<Context, string, IResponse> SignupError { get; set; }
        public Func<Context, User, IResponse> SignUpOK { get; set; }

        public HtmlUserExposer(UserManager userManager)
        {
            manager = userManager;
        }

        public dynamic WalledGarden(Context context)
        {
            if (!manager.WalledGarden) return false;

            if (context.Session<User>("user") == null && context.Request.Url.LocalPath != "/signin" && context.Request.HttpMethod != "POST")
            {
                if (manager.Host.PublicFolders.Any(y => y.Map(context)))
                    return false;

                return SignInForm(context);
            }

            return false;
        }

        public IResponse SignIn(Context context, string user, string domain, string pass)
		{
            if (string.IsNullOrWhiteSpace(user))
                return SignInError(context, "Username required");

            if (string.IsNullOrWhiteSpace(pass))
                return SignInError(context, "Password required");

            var u = manager.SignIn(string.IsNullOrEmpty(domain) ? user : user + "@" + domain, pass);

            if (u == null)
                return SignInError(context,"Invalid user name or password.");

            context.Session("user", u);
            return SignInOK(context);
		}
		
        public IResponse SignOut(Context cnt)
        {
            cnt.Session("user", null);
            return SignOutOK(cnt);
        }

        public IResponse SignedUp(Context cnt, string displayName, string domain, string email, string username, string password )
        {
            if (manager.Configuration.MandatoryDisplayName && string.IsNullOrWhiteSpace(displayName))
                return SignupError(cnt,"Display name is mandatory");

            displayName = displayName.HTMLEncode();

            if (manager.Configuration.MandatoryDomain && string.IsNullOrWhiteSpace(domain))
                return SignupError(cnt, "Domain is mandatory");

            domain = domain.HTMLEncode();

            if (manager.Configuration.MandatoryEmail)
                return SignupError(cnt, "Email is mandatory");

            if (manager.Configuration.MandatoryEmail && !EmailValidator.Validate(email))
                return SignupError(cnt, "Email is invalid");

            //FIXME: ADD PROOF OF MAIL REAL EXIST (SEND A MAIL)
            email = email.HTMLEncode();


            if (string.IsNullOrWhiteSpace(username))
                return SignupError(cnt,"Username is mandatory");

            if (string.IsNullOrWhiteSpace(password))
                return SignupError(cnt, "Password is mandatory");

            username = username.HTMLEncode();

            var user = new User
            {
                DisplayName = displayName,
                Domain = domain,
                Email = email,
                UserName = username
            };

            if (manager.Exists(user))
                return SignupError(cnt, "Username already taken");

            if(!manager.Add(user, password, manager.System))
                return SignupError(cnt, "Something went wrong");

            return SignUpOK(cnt,user);
        }
    }
}
