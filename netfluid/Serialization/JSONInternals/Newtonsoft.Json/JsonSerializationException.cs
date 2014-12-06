using System;
namespace Newtonsoft.Json
{
	internal class JsonSerializationException : JsonException
	{
		internal JsonSerializationException()
		{
		}
		internal JsonSerializationException(string message) : base(message)
		{
		}
		internal JsonSerializationException(string message, Exception innerException) : base(message, innerException)
		{
		}
		internal static JsonSerializationException Create(JsonReader reader, string message)
		{
			return JsonSerializationException.Create(reader, message, null);
		}
		internal static JsonSerializationException Create(JsonReader reader, string message, Exception ex)
		{
			return JsonSerializationException.Create(reader as IJsonLineInfo, reader.Path, message, ex);
		}
		internal static JsonSerializationException Create(IJsonLineInfo lineInfo, string path, string message, Exception ex)
		{
			message = JsonPosition.FormatMessage(lineInfo, path, message);
			return new JsonSerializationException(message, ex);
		}
	}
}
