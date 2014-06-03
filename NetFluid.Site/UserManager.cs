using System;
using System.Linq;
using NetFluid.Mongo;

namespace NetFluid.Site
{
    [VirtualHost("user.netfluid.org")]
    public class UserManager:FluidPage
    {
        public static Repository<User> Users;

        static UserManager()
        {
            Users = new Repository<User>("mongodb://localhost", "NetFluidSite");

            if (Users.Any()) return;

            var user = new User
            {
                Name = "theqult",
                Password = Security.Sha1("Labello1"),
                Domain = "netfluid.org"
            };
            user.Groups.Add("admin");
            user.IP.Add("127.0.0.1");
            user.SignIn.Add(DateTime.Now);
            Users.Save(user);
        }

        [Route("signup")]
        public IResponse SignUp(string username, string password)
        {
            var user = Users.FirstOrDefault(x => x.Name == username);

            if (user != null)
                return new FluidTemplate("embed:NetFluid.Site.UI.signup.html", "User name already in use");

            user = new User
            {
                Name = username,
                Password = Security.Sha1(password)
            };
            user.IP.Add(Context.RemoteEndPoint.Address.ToString());
            user.SignIn.Add(DateTime.Now);
            Users.Save(user);

            Session("user", user);

            return new RedirectResponse("http://netfluid.org/");
        }

        [Route("signin")]
        public IResponse SignIn(string username, string password)
        {
            var user = Users.FirstOrDefault(x => x.Name == username && x.Password == Security.Sha1(password));

            if (user!=null)
            {
                user.IP.Add(Context.RemoteEndPoint.Address.ToString());
                user.SignIn.Add(DateTime.Now);
                Users.Save(user);

                Session("user", user);
            }
            return new RedirectResponse("http://netfluid.org/");
        }

        [Route("signout")]
        public IResponse SignOut()
        {
            Context.Session("user", null);
            return new RedirectResponse("http://netfluid.org/");
        }
    }
}
