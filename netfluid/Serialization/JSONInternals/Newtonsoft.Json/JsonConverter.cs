using Newtonsoft.Json.Schema;
using System;
namespace Newtonsoft.Json
{
	internal abstract class JsonConverter
	{
		internal virtual bool CanRead
		{
			get
			{
				return true;
			}
		}
		internal virtual bool CanWrite
		{
			get
			{
				return true;
			}
		}
		internal abstract void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);
		internal abstract object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);
		internal abstract bool CanConvert(Type objectType);
		internal virtual JsonSchema GetSchema()
		{
			return null;
		}
	}
}
