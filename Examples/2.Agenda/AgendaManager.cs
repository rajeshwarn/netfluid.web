using NetFluid;

namespace Agenda
{
    internal class AgendaManager : FluidPage
    {
        [Route("/")]
        public object Home()
        {
            return new FluidTemplate("index.html");
        }

        [Route("/update")]
        public object Update(Person[] person, string[] name, string[] surname, string[] mail, string[] tel)
        {
            if (person == null || name == null || surname == null || mail == null || tel == null)
                return new FluidTemplate("index.html");

            int length = person.Length;
            if (name.Length == length && surname.Length == length && mail.Length == length && tel.Length == length)
            {
                for (int i = 0; i < length; i++)
                {
                    person[i].Name = name[i];
                    person[i].Surname = surname[i];
                    person[i].Email = mail[i];
                    person[i].Telephone = tel[i];

                    Person.Save(person[i]);
                }
            }

            return new FluidTemplate("index.html");
        }
    }
}