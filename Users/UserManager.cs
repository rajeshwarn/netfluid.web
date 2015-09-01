
using Netfluid.DB;
using System;
using System.Linq;

namespace Netfluid.Users
{

    public class UserManager
    {
        private const string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM123456789!£$%&/()=?^+;,:.-";


        public HtmlUserExposer Exposer { get; private set; }
        public Host Host { get; private set; }
        public LiteCollection<User> Repository { get; set; }
        public int SignInInterval { get; set; }
    
        public bool WalledGarden { get; set; }
        public Configuration Configuration { get; set; }
        
        internal User System { get; private set; } 

        public UserManager()
		{
            var db = new LiteDatabase("users.db");
            Repository = db.GetCollection<User>("user");

            SignInInterval = 10;

			if (!Repository.Any())
			{
				User admin = new User
				{
					DisplayName = "Administrator",
					GlobalAdmin = true,
					UserName = "root"
				};

				SaltHim(admin, "root");

                User sys = new User
                {
                    DisplayName = "System",
                    GlobalAdmin = true,
                    UserName = "system"
                };

                SaltHim(sys, Guid.NewGuid().ToString());
            }

            System = GetUser("system");
            Exposer = new HtmlUserExposer(this);
		}

        public void Mount(Host host,string mountPoint)
        {
            this.Host = host;

            if (mountPoint[0] != '/') mountPoint = "/" + mountPoint;
            if (mountPoint[mountPoint.Length - 1] != '/') mountPoint = mountPoint + "/";

            Host.Routes["GET", mountPoint+"signin"] = Route.New(Exposer.SignInForm);
            Host.Routes["POST", mountPoint + "signin"] = Route.New(new Func<Context, string, string, string, IResponse>(Exposer.SignIn));

            Host.Routes["GET", mountPoint + "signout"] = Route.New(new Func<Context,IResponse>(Exposer.SignOut));

            Host.Routes["GET", mountPoint + "signup"] = Route.New(Exposer.SignUpForm);
            Host.Routes["POST", mountPoint + "signup"] = Route.New(new Func<Context, string, string, string,string,string, IResponse>(Exposer.SignedUp));
        }

        void SaltHim(User user, string password)
        {
            string salt = new string(charset.Random(32).ToArray<char>());
            int rounds = Security.Random(4000);
            Func<string, string>[] methods = new Func<string, string>[]
            {
                new Func<string, string>(Security.SHA1),
                new Func<string, string>(Security.SHA256),
                new Func<string, string>(Security.SHA384),
                new Func<string, string>(Security.SHA512)
            };
            Func<string, string> method = methods.Random<Func<string, string>>();
            string newPassword = "";
            for (int i = 0; i < rounds; i++)
            {
                newPassword = method(password + salt);
            }

            user.Salt = salt;
            user.Round = rounds;
            user.Method = method.Method.Name;
            user.Password = newPassword;

            if (!Repository.Any(x => x.Domain == user.Domain && x.UserName == user.UserName))
            {
                Repository.Insert(user);
            }
            Repository.Update(user);
        }

		public User GetUser(string name)
		{
            if (string.IsNullOrEmpty(name)) return null;

            int index = name.IndexOf('@');
            string user = (index > 0) ? name.Substring(0, index) : name;
            string domain = (index >= 0) ? name.Substring(index) : null;

            return Repository.FirstOrDefault(x=>x.UserName == user && x.Domain == domain);
        }

        public bool Exists(string fullname)
        {
            return Exists(GetUser(fullname));
        }

        public bool Exists(User user)
		{
            if (user == null) return false;

            return Repository.Any(x => x.UserName == user.UserName && x.Domain == user.UserName);
        }

        public bool CheckAuthority(string user, string auth)
		{
			return CheckAuthority(GetUser(user), GetUser(auth));
		}

        public bool CheckAuthority(User user, User auth)
		{
            if (!Exists(user) || !Exists(auth)) return false;

            if (auth.GlobalAdmin) return true;

            if(user.Domain != null && auth.Domain != null)
                return auth.DomainAdmin && (auth.Domain == user.Domain || user.Domain.EndsWith("." + auth.Domain));

            return false;
		}

		public User SignIn(string fullname, string pass)
		{
            User user = GetUser(fullname);

			User result;
            if (user == null || (DateTime.Now - user.LastLogin).TotalSeconds <= SignInInterval)
                return null;

            Func<string, string> method;

            switch (user.Method)
            {
                case "SHA1": method = new Func<string, string>(Security.SHA1); break;
                case "SHA256": method = new Func<string, string>(Security.SHA256); break;
                case "SHA384": method = new Func<string, string>(Security.SHA384); break;
                case "SHA512": method = new Func<string, string>(Security.SHA512); break;
                default:
                    throw new Exception("Invalid cryptography method into user database");
            }

            string newPassword = "";
            for (int i = 0; i < user.Round; i++)
            {
                newPassword = method(pass + user.Salt);
            }
            if (user.Password == newPassword)
            {
                user.LastLogin = DateTime.Now;

                Repository.Update(user);

                result = user;
                return result;
            }
            result = null;
            return result;
        }

        public bool Add(User user, string password, User auth)
		{
            if (!Exists(user) || !CheckAuthority(user, auth)) return false;

            SaltHim(user, password);
            return true;
        }

        public bool Remove(User user, User auth)
		{
            if (!CheckAuthority(user, auth)) return false;

            Repository.Delete(x => x.Domain == user.Domain && x.UserName == user.UserName);
            return true;
        }

		public bool ChangePassword(User user, User auth, string newPassword)
		{
            if (!CheckAuthority(user, auth)) return false;
            SaltHim(user, newPassword);
            return true;
		}
    }
}
