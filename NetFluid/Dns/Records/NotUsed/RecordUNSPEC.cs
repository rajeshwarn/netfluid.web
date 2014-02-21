/*

 */

namespace NetFluid.DNS.Records
{
	public class RecordUNSPEC : Record
	{
		public byte[] RDATA;

		public override string ToString()
		{
			return string.Format("not-used");
		}

	}
}
