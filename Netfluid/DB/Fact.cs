using System;

namespace Netfluid.DB
{
    public class Fact
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public string Description { get; set; }

        public Fact()
        {
            DateTime = DateTime.Now;
        }
    }
}
