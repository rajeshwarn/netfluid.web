using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetFluid;
using System.Linq;

namespace NetFluidService
{
    public class UserInterface:FluidPage
    {
        private const string SignInErrorMessage = "Bad user name or password";
        private static readonly char[] alphabet;

        static UserInterface()
        {
            alphabet = "qwertyuiopasdfghjklzxcvbnm.1234567890".ToCharArray();
        }

        [Route("/changepwd")]
        public FluidTemplate ChangePwd(string password)
        {
            var u = Session<User>("user");
            if (u != null)
                u.ChangePassword(password);

            Session("user", u);
            return new FluidTemplate("./UI/Index.html");
        }

        [Route("/signin")]
        public FluidTemplate SignIn(string name,string password)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(password))
            {
                var user = User.Validate(name, password);

                if (user == null)
                    return new FluidTemplate("./Users/UI/SignIn.html", SignInErrorMessage);

                if (user.Status == User.UserStatus.Banned)
                    return new FluidTemplate("./Users/UI/SignIn.html", "Congratulations ! You had been banned :D");

                Session("user", user);

                return new FluidTemplate("./UI/index.html");
            }
            return new FluidTemplate("./Users/UI/SignIn.html");
        }

        [Route("/signup")]
		public FluidTemplate SignUp (string action,string display, string name, string password)
		{
			if (action == "signup")
			{
				if (string.IsNullOrEmpty (name))
                    return new FluidTemplate("./Users/UI/SignUp.html", "User name required");

				if (string.IsNullOrEmpty (password))
                    return new FluidTemplate("./Users/UI/SignUp.html", "Password required");

			    if (name.Any(c => !alphabet.Contains(c)))
			        return new FluidTemplate("./Users/UI/SignUp.html", "User name valid characters: lower case, dot, digits");

			    if (User.Exist(name))
                    return new FluidTemplate("./Users/UI/SignUp.html", "User name already in use");

                if (string.IsNullOrEmpty(display))
                    return new FluidTemplate("./Users/UI/SignUp.html", "Display name required");

                var user = new User { Name = name, NiceName = HTMLEncode(display), Password = password, Status = User.UserStatus.Active };
                User.Save(user);
                Session("user",user);

                return new FluidTemplate("./UI/index.html");
			}
			return new FluidTemplate("./Users/UI/SignUp.html");
        }
        
        [Route("/signout")]
        public FluidTemplate SignOut()
        {
            Session("user", null);
            return new FluidTemplate("./UI/index.html", "You need to verify your mail before continue");
        }
    }
}
