// Stuff records are made of

namespace NetFluid.DNS.Records
{
	public abstract class Record
	{
		/// The name of the node to which this resource record pertains
		/// </summary>
		public string Name;

		/// <summary>
		/// Specifies type class of resource record, mostly IN but can be CS, CH or HS 
		/// </summary>
		public Class Class;

	    /// <summary>
	    /// Time to live, the time interval that the resource record may be cached
	    /// </summary>
	    public uint TTL { get; set; }


		public int TimeLived;

	    protected Record()
        {
            TimeLived = 0;
            Name = "netfluid.org";
            Class = Class.IN;
            TTL = int.MaxValue - 1;
        }
	}
}
