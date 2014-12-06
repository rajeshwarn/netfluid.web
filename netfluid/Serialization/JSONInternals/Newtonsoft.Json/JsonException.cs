using System;
namespace Newtonsoft.Json
{
	internal class JsonException : Exception
	{
		internal JsonException()
		{
		}
		internal JsonException(string message) : base(message)
		{
		}
		internal JsonException(string message, Exception innerException) : base(message, innerException)
		{
		}
		internal static JsonException Create(IJsonLineInfo lineInfo, string path, string message)
		{
			message = JsonPosition.FormatMessage(lineInfo, path, message);
			return new JsonException(message);
		}
	}
}
