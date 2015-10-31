
using Netfluid.DB;
using System;
using System.ComponentModel;
using System.Linq;

namespace Netfluid.Users
{
    public class UserManager<T> where T :User
    {
        UserExposer<T> exposer;
        const string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM123456789!£$%&/()=?^+;,:.-";

        public UserManager()
        {
        }

        public UserManager(IKeyValueStore<T> repository)
        {
            Repository = repository;
        }

        public NetfluidHost Host { get; private set; }

        [DefaultValue(10)]
        public int SignInInterval { get; set; }

        [DefaultValue(false)]
        public bool WalledGarden { get; set; }

        public T System { get; private set; }

        public IKeyValueStore<T> Repository { get; set; }

        public void Setup(NetfluidHost host)
		{
            Host = host;
            exposer = new UserExposer<T>(host, this);
        }

        void SaltHim(T user, string password)
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
                Repository.Insert(user.UserName,user);
                return;
            }
            Repository.Update(user.UserName,user);
        }

		public T GetUser(string name)
		{
            if (string.IsNullOrEmpty(name)) return null;

            return Repository.Get(name);
        }

        public bool Exists(T user)
		{
            if (user == null) return false;

            return Repository.Get(user.UserName) != null;
        }

        public bool CheckAuthority(string user, string auth)
		{
			return CheckAuthority(GetUser(user), GetUser(auth));
		}

        public bool CheckAuthority(T user, T auth)
		{
            if (!Exists(user) || !Exists(auth)) return false;

            if (auth.GlobalAdmin) return true;

            if(user.Domain != null && auth.Domain != null)
                return auth.DomainAdmin && (auth.Domain == user.Domain || user.Domain.EndsWith("." + auth.Domain));

            return false;
		}

		public T SignIn(string fullname, string pass)
		{
            T user = GetUser(fullname);

			T result;
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

                Repository.Update(user.UserName, user);

                result = user;
                return result;
            }
            result = null;
            return result;
        }

        public bool Add(T user, string password)
		{
            SaltHim(user, password);
            return true;
        }

        public bool Remove(T user, T auth)
		{
            if (!CheckAuthority(user, auth)) return false;

            Repository.Delete(user.UserName);
            return true;
        }

		public bool ChangePassword(T user, T auth, string newPassword)
		{
            if (!CheckAuthority(user, auth)) return false;
            SaltHim(user, newPassword);
            return true;
		}
    }
}
