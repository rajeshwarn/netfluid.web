
using Netfluid.DB;
using System;
using System.Linq;

namespace Netfluid.Users
{
	public class UserManager : MethodExposer
	{
		private const string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM123456789!£$%&/()=?^+;,:.-";

        static LiteCollection<User> Repository { get; set; }

		static UserManager()
		{
            var db = new LiteDatabase("users.db");
            Repository = db.GetCollection<User>("user");

			if (!Repository.Any())
			{
				User admin = new User
				{
					DisplayName = "Administrator",
					GlobalAdmin = true,
					UserName = "root"
				};

				SaltHim(admin, "lullabi");
				User system = new User
				{
					DisplayName = "System",
					GlobalAdmin = true,
					UserName = "system"
				};

                SaltHim(system, new string(charset.Random(356).ToArray()));
			}

			Engine.DefaultHost.Load(typeof(UserExposer));
		}

        public bool WalledGarden
        {
            get
            {
                return Engine.Host(Host.Name).Filters.Any(x=>x.Name == "Netfluid.User.WalledGarden");
            }
            set
            {
                if(value)
                {
                    Engine.Host(Host.Name).Filters.Add(new Filter
                    {
                        Name = "Netfluid.User.WalledGarden",
                        MethodInfo = typeof(WalledGarden).GetMethod("CheckSignedIn")
                    });
                }
            }
        }

		public static User GetUser(string name)
		{
			User result;
			if (string.IsNullOrEmpty(name))
			{
				result = null;
			}
			else
			{
				int index = name.IndexOf('@');
				string user = (index > 0) ? name.Substring(0, index) : name;
				string domain = (index >= 0) ? name.Substring(index) : null;
				result = Repository.FirstOrDefault(Query.And(Query.EQ("UserName", user), Query.EQ("Domain", domain)));
            }
			return result;
		}
        
        public static bool CheckExists(User user)
		{
            return Repository.Exists(Query.And(Query.EQ("UserName",user.UserName),Query.EQ("Domain",user.Domain)));
		}

        public static bool CheckAuthority(string user, string auth)
		{
			return CheckAuthority(GetUser(user), GetUser(auth));
		}

        public static bool CheckAuthority(User user, User auth)
		{
			return user != null && auth != null && CheckExists(auth) && (auth.GlobalAdmin || (auth.DomainAdmin && auth.Domain == user.Domain && !user.Domain.EndsWith("." + auth.Domain)));
		}

		public static void SaltHim(User user, string password)
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

            if (!Repository.Exists(x => x.Domain == user.Domain && x.UserName == user.UserName))
            {
                Repository.Insert(user);
            }
            Repository.Update(user);
        }

        public static User SignIn(string name, string pass)
        {
            if (name.Contains('@'))
            {
                var username = name.Substring(0, name.IndexOf('@'));
                var domain = name.Substring(name.IndexOf('@') + 1);
                return SignIn(name, domain, pass);
            }
            return SignIn(name,null,pass);
        }


		public static User SignIn(string name, string domain, string pass)
		{
			User user;

            if (name.Contains('@'))
            {
                var username = name.Substring(0, name.IndexOf('@'));
                domain = name.Substring(name.IndexOf('@') + 1);
                name = username;
            }

			if (domain == null)
			{
				user = Repository.FirstOrDefault(Query.EQ("UserName", name));
            }
			else
			{
				user = Repository.FirstOrDefault(Query.And(Query.EQ("UserName", name), Query.EQ("Domain", domain)));
            }

			User result;
			if (user == null || (DateTime.Now - user.LastLogin).TotalSeconds <= 10.0)
			{
				result = null;
			}
			else
			{
				string method2 = user.Method;
				if (method2 != null)
				{
					Func<string, string> method;
					if (!(method2 == "SHA1"))
					{
						if (!(method2 == "SHA256"))
						{
							if (!(method2 == "SHA384"))
							{
								if (!(method2 == "SHA512"))
								{
									goto IL_217;
								}
								method = new Func<string, string>(Security.SHA512);
							}
							else
							{
								method = new Func<string, string>(Security.SHA384);
							}
						}
						else
						{
							method = new Func<string, string>(Security.SHA256);
						}
					}
					else
					{
						method = new Func<string, string>(Security.SHA1);
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
				IL_217:
				result = null;
			}
			return result;
		}

        public static bool AddDomainAdministrator(string domain, string display, string name, string password, User auth)
		{
			User user = new User
			{
				Domain = domain,
				DisplayName = display,
				UserName = name,
				DomainAdmin = true
			};
			bool result;
			if (!CheckExists(user) || !CheckAuthority(user, auth))
			{
				result = false;
			}
			else
			{
				SaltHim(user, password);
				result = true;
			}
			return result;
		}

        public static bool AddUser(string domain, string display, string name, string password, User auth)
		{
			User user = new User
			{
				Domain = domain,
				DisplayName = display,
				UserName = name
			};
			bool result;
			if (!CheckExists(user) || !CheckAuthority(user, auth))
			{
				result = false;
			}
			else
			{
				SaltHim(user, password);
				result = true;
			}
			return result;
		}

        public static bool Remove(User user, User auth)
		{
			bool result;
			if (!CheckAuthority(user, auth))
			{
				result = false;
			}
			else
			{
				if (!CheckExists(user))
				{
					result = true;
				}
				else
				{
                    Repository.Delete(x => x.Domain == user.Domain && x.UserName == user.UserName);
					result = true;
				}
			}
			return result;
		}

		public static bool ChangePassword(User user, User auth, string newPassword)
		{
			bool result;
			if (!CheckAuthority(user, auth))
			{
				result = false;
			}
			else
			{
				if (!CheckExists(user))
				{
					result = false;
				}
				else
				{
					SaltHim(user, newPassword);
					result = true;
				}
			}
			return result;
		}
    }
}
