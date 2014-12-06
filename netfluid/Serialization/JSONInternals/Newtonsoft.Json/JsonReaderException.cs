using System;
namespace Newtonsoft.Json
{
	internal class JsonReaderException : JsonException
	{
		internal int LineNumber
		{
			get;
			private set;
		}
		internal int LinePosition
		{
			get;
			private set;
		}
		internal string Path
		{
			get;
			private set;
		}
		internal JsonReaderException()
		{
		}
		internal JsonReaderException(string message) : base(message)
		{
		}
		internal JsonReaderException(string message, Exception innerException) : base(message, innerException)
		{
		}
		internal JsonReaderException(string message, Exception innerException, string path, int lineNumber, int linePosition) : base(message, innerException)
		{
			this.Path = path;
			this.LineNumber = lineNumber;
			this.LinePosition = linePosition;
		}
		internal static JsonReaderException Create(JsonReader reader, string message)
		{
			return JsonReaderException.Create(reader, message, null);
		}
		internal static JsonReaderException Create(JsonReader reader, string message, Exception ex)
		{
			return JsonReaderException.Create(reader as IJsonLineInfo, reader.Path, message, ex);
		}
		internal static JsonReaderException Create(IJsonLineInfo lineInfo, string path, string message, Exception ex)
		{
			message = JsonPosition.FormatMessage(lineInfo, path, message);
			int lineNumber;
			int linePosition;
			if (lineInfo != null && lineInfo.HasLineInfo())
			{
				lineNumber = lineInfo.LineNumber;
				linePosition = lineInfo.LinePosition;
			}
			else
			{
				lineNumber = 0;
				linePosition = 0;
			}
			return new JsonReaderException(message, ex, path, lineNumber, linePosition);
		}
	}
}
