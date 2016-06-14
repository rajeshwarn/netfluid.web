using System;

namespace Netfluid.DB
{
	/// <summary>
	/// Helper class contains static methods that read and write numeric values
	/// into and from a byte array in little endian byte order.
	/// </summary>
	static class BufferHelper
	{
		public static uint ReadBufferUInt32 (byte[] buffer, int bufferOffset)
		{
			var uintBuffer = new byte[4];
			Buffer.BlockCopy (buffer, bufferOffset, uintBuffer, 0, 4);
			return BitConverter.ToUInt32 (uintBuffer,0);
		}

		public static int ReadBufferInt32 (byte[] buffer, int bufferOffset)
		{
			var intBuffer = new byte[4];
			Buffer.BlockCopy (buffer, bufferOffset, intBuffer, 0, 4);
			return BitConverter.ToInt32 (intBuffer,0);
		}

        public static long ReadBufferInt64 (byte[] buffer, int bufferOffset)
		{
			var longBuffer = new byte[8];
			Buffer.BlockCopy (buffer, bufferOffset, longBuffer, 0, 8);
			return BitConverter.ToInt64 (longBuffer,0);
		}

		public static double ReadBufferDouble (byte[] buffer, int bufferOffset)
		{
			var doubleBuffer = new byte[8];
			Buffer.BlockCopy (buffer, bufferOffset, doubleBuffer, 0, 8);
			return BitConverter.ToDouble (doubleBuffer,0);
		}

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

