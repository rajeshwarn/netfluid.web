using System.IO;

namespace Netfluid.DB
{
    static class StreamExtension
	{
		/// <summary>
		/// Read until given buffer is filled or end of stream is reached
		/// </summary>
		public static int Read (this Stream src, byte[] buffer)
		{
			var filled = 0;
			var lastRead = 0;
			while (filled < buffer.Length)
			{
				lastRead = src.Read (buffer, filled, buffer.Length - filled);
				filled += lastRead;
				if (lastRead == 0) break;
			}

			return filled;
		}
	}
}

