using System.Linq;

namespace Netfluid.Users
{
	class UserExposer:MethodExposer
	{
        UserManager manager;

        public UserExposer(UserManager userManager)
        {
            manager = userManager;
        }

		public IResponse SignIn()
		{
			return new MustacheTemplate("./Users/signIn.html");
		}

        public bool WalledGarden(ref IResponse resp)
        {
            if (base.Session<User>("user") == null && base.Request.Url.LocalPath != "/signin" && base.Request.HttpMethod != "POST")
            {
                if (Engine.Hosts.Any(x => x.PublicFolders.Any(y => y.Map(Context))))
                    return false;

                resp = new MustacheTemplate("./Users/signIn.html", new { Redirect = Request.Url.LocalPath });
                return true;
            }

            return false;
        }

        public IResponse SignIn(string user, string domain, string pass)
		{
			IResponse result;
			if (user == null)
			{
				result = new MustacheTemplate("./Users/signIn.html", new
				{
					error = "User name required"
				});
			}
			else
			{
				if (pass == null)
				{
					result = new MustacheTemplate("./Users/signIn.html", new
					{
						error = "Password required"
					});
				}
				else
				{
                    var u = manager.SignIn(string.IsNullOrEmpty(domain) ? user : user + "@" + domain, pass);

					if (u != null)
					{
						base.Session("user", u);
						result = new RedirectResponse(base.Context.Values.Contains("redirect") ? base.Context.Values["redirect"] : "/");
					}
					else
					{
						result = new MustacheTemplate("./Users/signIn.html", new
						{
							error = "Invalid user name or password. Please retry after 10 seconds."
						});
					}
				}
			}
			return result;
		}
		
		public IResponse SignInApi(string user, string domain, string pass)
		{
			IResponse result;
			if (user == null)
			{
				base.Response.StatusCode = 400;
				result = new JSONResponse(new
				{
					error = "User name required"
				});
			}
			else
			{
				if (pass == null)
				{
					base.Response.StatusCode = 400;
					result = new JSONResponse(new
					{
						error = "Password required"
					});
				}
				else
				{
                    var u = manager.SignIn(string.IsNullOrEmpty(domain) ? user : user + "@" + domain, pass);
                    if (u != null)
					{
						base.Session("user", u);
						result = new JSONResponse(new
						{
							userName = u.UserName,
							displayName = u.DisplayName,
							domain = u.Domain,
							domainAdmin = u.DomainAdmin,
							globalAdmin = u.GlobalAdmin,
							lastLogin = u.LastLogin,
							lastUpdate = u.LastModify
						});
					}
					else
					{
						base.Response.StatusCode = 400;
						result = new JSONResponse(new
						{
							error = "Invalid user name or password. Please retry after 10 seconds."
						});
					}
				}
			}
			return result;
		}

        public IResponse SignOut()
        {
            Session("user", null);
            return new RedirectResponse("/");
        }
	}
}
