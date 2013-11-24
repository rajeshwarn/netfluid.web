using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace NetFluidService
{
    [Serializable]
    public class User
    {
        [BsonId]
        public ObjectId _id { get; set; }

        public string Name { get; set; }

        public string NiceName { get; set; }

        public string Password { get; set; }

        public UserStatus Status { get; set; }

        public string Avatar { get; set; }

        public string Mail { get; set; }

        [Serializable]
        public enum UserStatus
        {
            InActive,
            Active,
            Banned,
            Admin
        }

        [NonSerialized]
        static readonly MongoDatabase db;

        static User()
        {
            var client = new MongoClient("mongodb://localhost");
            var server = client.GetServer();
            db = server.GetDatabase("netfluid");

            if (Collection.FindOne()==null)
            {
                var anon = new User { Name = "anonymous", NiceName = "Anonymous", Mail = "anon@netfluid.org"};
                var nf = new User { Name = "netfluid", NiceName = "NetFluid", Mail ="netfluid@netfluid.org", Status = User.UserStatus.Admin, Password = "ludmilla" };

                Collection.Save(anon);
                Collection.Save(nf);
            }
        }

        static MongoCollection<User> Collection
        {
            get { return db.GetCollection<User>("User"); }
        }

        public User()
        {
            Name = string.Empty;
            NiceName = string.Empty;
            Password = string.Empty;
            Avatar = "default.png";
        }

        public override string ToString()
        {
            return NiceName;
        }

        public bool IsActive
        {
            get { return  Status == UserStatus.Active || Status == UserStatus.Admin;}
        }

        public bool Banned
        {
            get { return  Status == UserStatus.Banned;}
        }

        public bool Admin
        {
            get { return  Status == UserStatus.Admin;}
        }

        public void ChangePassword(string newpwd)
        {
            this.Password = NetFluid.Security.Sha1(newpwd);
            Collection.Save(this);
        }

        public static bool IsBanned(User usr)
        {
            return usr != null && usr.Status == UserStatus.Banned;
        }

        public static bool IsAdmin(User usr)
        {
            return usr != null && usr.Status == UserStatus.Admin;
        }

        public static void Save(User user)
        {
            Collection.Save(user);
        }

        public static User Parse(string name)
        {
            return Collection.FindOne(Query.EQ("Name",name.ToLowerInvariant()));
        }

        public static bool Exist(string name)
        {
            return Collection.FindOne(Query.EQ("Name", name)) != null;
        }

        public static User ByMail(string mail)
        {
            return Collection.FindOne(Query.EQ("Mail", mail));
        }

        public static User Validate(string username, string password)
        {
            if (password == null)
                password = string.Empty;

            if (username == null)
                username = string.Empty;

            if (username == "anonymous")
                return Parse("anonymous");

            return Collection.FindOne(Query.And(Query.EQ("Name", username),Query.EQ("Password",password)));
        }
    }
}