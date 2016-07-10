using System;

namespace Netfluid.DB.Serializers
{
	internal class LongSerializer : ISerializer<long>
	{
		public byte[] Serialize (long value)
		{
			return BitConverter.GetBytes (value);
		}

		public long Deserialize (byte[] buffer, int offset, int length)
		{
			if (length != 8) {
				throw new ArgumentException ("Invalid length: " + length);
			}
			
			return BitConverter.ToInt64 (buffer, offset);
		}

		public bool IsFixedSize {
			get {
				return true;
			}
		}

		public int Length {
			get {
				return 8;
			}
		}
	}
}

