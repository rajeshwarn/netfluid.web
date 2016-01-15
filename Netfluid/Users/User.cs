using System;

namespace Netfluid.Users
{
    public class User
	{
        public string DisplayName { get; set; }
        public string Email { get; set; }
		public string UserName { get; set; }
        public string Domain { get; set; }

        public CountryCode Country { get; set; }

        [IgnoreUpdate]
        public bool DomainAdmin { get; set; }

        [IgnoreUpdate]
        public bool GlobalAdmin { get; set; }

        [IgnoreUpdate]
        public string Password { get; set; }

        [IgnoreUpdate]
        public DateTime LastLogin { get; set; }

        [IgnoreUpdate]
        public DateTime LastModify { get; set; }

        [IgnoreUpdate]
        public string Salt { get; set; }

        [IgnoreUpdate]
        public int Round { get; set; }

        [IgnoreUpdate]
        public string Method { get; set; }

        public User()
        {
            UserName = "anon";
            DisplayName = "Anonymous";
        }

        public User(string fullname)
        {
            int index = fullname.IndexOf('@');
            UserName = (index > 0) ? fullname.Substring(0, index) : fullname;
            Domain = (index >= 0) ? fullname.Substring(index) : null;
        }

        public override string ToString()
		{
            return UserName;
		}

        public static User Parse(string name)
		{
            return new User { UserName = name };
		}
    }
}
