
using System;
using System.Linq;

namespace Netfluid.Users
{
    class UserManager
    {
        private const string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM123456789!£$%&/()=?^+;,:.-";

        public static NetfluidHost Host { get; private set; }
        public static int SignInInterval { get; set; }
        public static bool WalledGarden { get; set; }
        public static User System { get; private set; }

        public static IMongoCollection<User> Repository { get; set; }

        static UserManager()
		{
            SignInInterval = 10;
            WalledGarden = true;

            Repository = Program.Database.GetCollection<User>("users");

            var task = Repository.CountAsync(x=>x.UserName == "root" || x.UserName=="system");
            task.Wait();

            if (task.Result == 0)
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

        public static User ByID(ObjectId id)
        {
            var task = Repository.Find(x => x._id == id).FirstOrDefaultAsync();
            task.Wait();
            return task.Result;
        }

        static void SaltHim(User user, string password)
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

            if (!Exists(user))
            {
                Repository.InsertOneAsync(user).Wait();
                return;
            }
            Repository.ReplaceOneAsync(x=>x._id==user._id,user);
        }

		public static User GetUser(string name)
		{
            if (string.IsNullOrEmpty(name)) return null;

            int index = name.IndexOf('@');
            string user = (index > 0) ? name.Substring(0, index) : name;
            string domain = (index >= 0) ? name.Substring(index) : null;

            var task = Repository.Find(x=>x.UserName == user && x.Domain == domain).FirstOrDefaultAsync();
            task.Wait();
            return task.Result;
        }

        public static bool Exists(string fullname)
        {
            return Exists(GetUser(fullname));
        }

        public static bool Exists(User user)
		{
            if (user == null) return false;

            var task = Repository.Find(x => x.UserName == user.UserName && x.Domain == user.Domain).FirstOrDefaultAsync();
            task.Wait();
            return task.Result != null;
        }

        public static bool CheckAuthority(string user, string auth)
		{
			return CheckAuthority(GetUser(user), GetUser(auth));
		}

        public static bool CheckAuthority(User user, User auth)
		{
            if (!Exists(user) || !Exists(auth)) return false;

            if (auth.GlobalAdmin) return true;

            if(user.Domain != null && auth.Domain != null)
                return auth.DomainAdmin && (auth.Domain == user.Domain || user.Domain.EndsWith("." + auth.Domain));

            return false;
		}

		public static User SignIn(string fullname, string pass)
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

                Repository.ReplaceOneAsync(x=>x._id == user._id, user);

                result = user;
                return result;
            }
            result = null;
            return result;
        }

        public static bool Add(User user, string password, User auth)
		{
            if (!Exists(user) || !CheckAuthority(user, auth)) return false;

            SaltHim(user, password);
            return true;
        }

        public static bool Remove(User user, User auth)
		{
            if (!CheckAuthority(user, auth)) return false;

            Repository.DeleteOneAsync(x => x.Domain == user.Domain && x.UserName == user.UserName);
            return true;
        }

		public static bool ChangePassword(User user, User auth, string newPassword)
		{
            if (!CheckAuthority(user, auth)) return false;
            SaltHim(user, newPassword);
            return true;
		}
    }
}
