
using Netfluid.DB;
using System;
using System.ComponentModel;
using System.Linq;

namespace Netfluid.Users
{
    class UserManager
    {
        UserExposer exposer;
        const string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM123456789!£$%&/()=?^+;,:.-";

        public NetfluidHost Host { get; private set; }

        [DefaultValue(10)]
        public int SignInInterval { get; set; }

        [DefaultValue(false)]
        public bool WalledGarden { get; set; }

        public User System { get; private set; }

        public IKeyValueStore<User> Repository { get; set; }

        public void Setup(NetfluidHost host)
		{
            Host = host;
            exposer = new UserExposer(host, this);

            if(Repository==null)
                Repository = new KeyValueStore<User>("users");

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
            Func<string, string> method = methods.Random();
            string newPassword = "";
            for (int i = 0; i < rounds; i++)
            {
                newPassword = method(password + salt);
            }

            user.Salt = salt;
            user.Round = rounds;
            user.Method = method.Method.Name;
            user.Password = newPassword;

            if (!Exists(user))
            {
                Repository.Insert(user.Fullname,user);
                return;
            }
            Repository.Update(user.Fullname,user);
        }

		public User GetUser(string name)
		{
            if (string.IsNullOrEmpty(name)) return null;

            return Repository.Get(name);
        }

        public bool Exists(User user)
		{
            if (user == null) return false;

            return Repository.Get(user.Fullname) != null;
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

                Repository.Update(user.Fullname, user);

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

            Repository.Delete(user.Fullname);
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
