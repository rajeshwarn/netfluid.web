using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NetFluid;

namespace Agenda
{
    [Serializable]
    public class Person
    {
        private const string filename = "my.db";
        private static readonly ConcurrentDictionary<string, Person> db;
        public string Email;
        public string Id;
        public string Name;
        public string Surname;
        public string Telephone;

        static Person()
        {
            if (File.Exists(filename))
            {
                db = db.FromBinary(File.ReadAllBytes(filename));
                return;
            }

            #region GENERATING RANDOM DATA

            db = new ConcurrentDictionary<string, Person>();

            var names = new[] {"Mattew", "Andrew", "Valery", "Helen", "Lory", "Walter", "July"};
            var surnames = new[] {"Smiths", "Bush", "Bold", "Trum", "Silver", "Walter", "Wright"};
            var host = new[] {"netfluid.org", "theqult.com", "csharpages.com", "supb.eu"};
            const string btel = "00039 393 914 86 91";

            for (int i = 0; i < 50; i++)
            {
                var p = new Person
                            {
                                Id = Security.UID(),
                                Name = names.Random(),
                                Surname = surnames.Random(),
                                Telephone = btel + i.ToString(CultureInfo.InvariantCulture)
                            };

                p.Email = (p.Name[0] + "." + p.Surname).ToLowerInvariant() + "@" + host.Random();

                Save(p);
            }

            #endregion
        }

        public static IEnumerable<Person> All
        {
            get { return db.Values; }
        }

        public static Person Parse(string id)
        {
            Person p;
            db.TryGetValue(id, out p);
            return p;
        }

        public static void Save(Person p)
        {
            lock (db)
            {
                db.AddOrUpdate(p.Id, p, (key, val) => val);
                File.WriteAllBytes(filename, db.ToBinary());
            }
        }

        public static void Delete(Person p)
        {
            lock (db)
            {
                if (db.TryRemove(p.Id, out p))
                    File.WriteAllBytes(filename, db.ToBinary());
            }
        }
    }
}