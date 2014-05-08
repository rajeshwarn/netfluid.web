// Stuff records are made of

using System;

namespace NetFluid.DNS.Records
{
    [Serializable]
    public abstract class Record
    {
        /// <summary>
        ///     Specifies type class of resource record, mostly IN but can be CS, CH or HS
        /// </summary>
        public Class Class;

        /// The name of the node to which this resource record pertains
        /// </summary>
        public string Name;


        public int TimeLived;

        protected Record()
        {
            TimeLived = 0;
            Name = "netfluid.org";
            Class = Class.IN;
            TTL = int.MaxValue - 1;
        }

        /// <summary>
        ///     Time to live, the time interval that the resource record may be cached
        /// </summary>
        public uint TTL { get; set; }
    }
}