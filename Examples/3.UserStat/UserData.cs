using NetFluid.Collections.Persistent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace _3.UserStat
{
    [Serializable]
    public class UserData
    {
        private static PersistentDictionary<string, UserData> db;

        static UserData()
        {
            db = new PersistentDictionary<string, UserData>("my.db");
        }

        public UserData()
        {
            Interactions = new List<Interaction>();
        }

        public string Id { get; set; }
        public string UserAgent { get; set; }
        public string Ip { get; set; }
        public string Referer { get; set; }
        public string[] Languages { get; set; }
        public List<Interaction> Interactions { get; set; }

        public static UserData Parse(string id)
        {
            UserData data;
            if (db.TryGetValue(id, out data))
                return data;

            return null;
        }

        public static void Save(UserData data)
        {
            db[data.Id] = data;
        }

        public static IEnumerable<UserData> All
        {
            get { return db.Values; }
        }
    }

    [Serializable]
    public class Interaction
    {
        public string Url { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
