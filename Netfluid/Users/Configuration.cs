using System.ComponentModel;

namespace Netfluid.Users
{
    public class Configuration
    {
        [DefaultValue(false)]
        public bool MandatoryDisplayName { get; set; }

        [DefaultValue(false)]
        public bool MandatoryDomain { get; set; }

        [DefaultValue(false)]
        public bool MandatoryEmail { get; set; }

        [DefaultValue(10)]
        public int SignInInterval { get; set; }

        [DefaultValue(false)]
        public bool WalledGarden { get; set; }
    }
}
