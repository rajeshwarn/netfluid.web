namespace Netfluid.Users
{
    [RouteAttribute("/user", null, 99999)]
	class UserExposer:MethodExposer
	{
		[RouteAttribute("/signin", "GET", 99999)]
		public IResponse SignIn()
		{
			return new MustacheTemplate("./Users/signIn.html");
		}
		
        [RouteAttribute("/signin", "POST", 99999)]
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
					User u = UserManager.SignIn(user, domain, pass);
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
		
        [RouteAttribute("/signin", "POST", 99999)]
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
					User u = UserManager.SignIn(user, domain, pass);
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

        [RouteAttribute("/signout")]
        public IResponse SignOut()
        {
            Session("user", null);
            return new RedirectResponse("/");
        }
	}
}
