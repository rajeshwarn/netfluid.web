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

        public bool DomainAdmin { get; set; }
        public bool GlobalAdmin { get; set; }

        public string Password { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastModify { get; set; }
        public string Salt { get; set; }
        public int Round { get; set; }
        public string Method { get; set; }

        public string Fullname
        {
            get
            {
                return (this.Domain == null) ? this.UserName : (this.UserName + "@" + this.Domain);
            }
        }

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
			return (this.Domain == null) ? this.UserName : (this.UserName + "@" + this.Domain);
		}

        public static User Parse(string name)
		{
            return new User(name);
		}
    }
}
