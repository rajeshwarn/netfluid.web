using System.Linq;

namespace Netfluid.Users
{
    internal class WalledGarden:MethodExposer
	{
		[FilterAttribute]
		public bool CheckSignedIn(ref IResponse resp)
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
	}
}
