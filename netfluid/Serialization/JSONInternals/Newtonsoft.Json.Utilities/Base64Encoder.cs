using System;
using System.IO;
namespace Newtonsoft.Json.Utilities
{
	internal class Base64Encoder
	{
		private const int Base64LineSize = 76;
		private const int LineSizeInBytes = 57;
		private readonly char[] _charsLine = new char[76];
		private readonly TextWriter _writer;
		private byte[] _leftOverBytes;
		private int _leftOverBytesCount;
		internal Base64Encoder(TextWriter writer)
		{
			ValidationUtils.ArgumentNotNull(writer, "writer");
			this._writer = writer;
		}
		internal void Encode(byte[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count > buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (this._leftOverBytesCount > 0)
			{
				int leftOverBytesCount = this._leftOverBytesCount;
				while (leftOverBytesCount < 3 && count > 0)
				{
					this._leftOverBytes[leftOverBytesCount++] = buffer[index++];
					count--;
				}
				if (count == 0 && leftOverBytesCount < 3)
				{
					this._leftOverBytesCount = leftOverBytesCount;
					return;
				}
				int num2 = Convert.ToBase64CharArray(this._leftOverBytes, 0, 3, this._charsLine, 0);
				this.WriteChars(this._charsLine, 0, num2);
			}
			this._leftOverBytesCount = count % 3;
			if (this._leftOverBytesCount > 0)
			{
				count -= this._leftOverBytesCount;
				if (this._leftOverBytes == null)
				{
					this._leftOverBytes = new byte[3];
				}
				for (int i = 0; i < this._leftOverBytesCount; i++)
				{
					this._leftOverBytes[i] = buffer[index + count + i];
				}
			}
			int num3 = index + count;
			int length = 57;
			while (index < num3)
			{
				if (index + length > num3)
				{
					length = num3 - index;
				}
				int num4 = Convert.ToBase64CharArray(buffer, index, length, this._charsLine, 0);
				this.WriteChars(this._charsLine, 0, num4);
				index += length;
			}
		}
		internal void Flush()
		{
			if (this._leftOverBytesCount > 0)
			{
				int count = Convert.ToBase64CharArray(this._leftOverBytes, 0, this._leftOverBytesCount, this._charsLine, 0);
				this.WriteChars(this._charsLine, 0, count);
				this._leftOverBytesCount = 0;
			}
		}
		private void WriteChars(char[] chars, int index, int count)
		{
			this._writer.Write(chars, index, count);
		}
	}
}