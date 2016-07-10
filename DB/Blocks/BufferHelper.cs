using System;

namespace Netfluid.DB
{
	/// <summary>
	/// Helper class contains static methods that read and write numeric values
	/// into and from a byte array in little endian byte order.
	/// </summary>
	static class BufferHelper
	{
		public static void WriteBuffer (uint value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (BitConverter.GetBytes(value), 0, buffer, bufferOffset, 4);
		}

		public static void WriteBuffer (long value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (BitConverter.GetBytes(value), 0, buffer, bufferOffset, 8);
		}
	}
}

