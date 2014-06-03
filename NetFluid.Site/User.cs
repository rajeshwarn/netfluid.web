using System;
using System.Collections.Generic;

namespace NetFluid.Site
{
    public class User : IDatabaseObject
    {
        public string Id { get; set; }

        public string Name;
        public string Domain;
        public string Password;

        public List<string> Groups;

        public List<DateTime> SignIn;
        public List<string> IP;

        public User()
        {
            Groups = new List<string>();
            SignIn = new List<DateTime>();
            IP = new List<string>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
