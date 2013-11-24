using NetFluid;

namespace Agenda
{
    internal class AgendaManager : FluidPage
    {
        [Route("/")]
        public object Home()
        {
            return new FluidTemplate("./UI/index.html");
        }

        [Route("/add")]
        public object Add(string name, string surname, string email, string tel)
        {
            if (name != null && surname != null && email != null && tel != null)
            {
                Person.Save(new Person
                                {
                                    Name = name,
                                    Email = email,
                                    Id = Security.UID(),
                                    Surname = surname,
                                    Telephone = tel
                                });
            }

            return new FluidTemplate("./UI/index.html");
        }

        [Route("/update")]
        public object Update(Person[] person, string[] name, string[] surname, string[] mail, string[] tel)
        {
            if (person == null || name == null || surname == null || mail == null || tel == null)
                return new FluidTemplate("./UI/index.html");

            int length = person.Length;
            if (name.Length == length && surname.Length == length && mail.Length == length && tel.Length == length)
            {
                for (int i = 0; i < length; i++)
                {
                    person[i].Name = name[i];
                    person[i].Surname = surname[i];
                    person[i].Email = mail[i];
                    person[i].Telephone = tel[i];
                }
            }

            return new FluidTemplate("./UI/index.html");
        }

        [Route("/delete")]
        public object Delete(Person[] person)
        {
            if (person != null)
                foreach (var p in person)
                    Person.Delete(p);

            return new FluidTemplate("./UI/index.html");
        }
    }
}