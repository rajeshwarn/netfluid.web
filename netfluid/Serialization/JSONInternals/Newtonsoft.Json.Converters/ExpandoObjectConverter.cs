using Newtonsoft.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
namespace Newtonsoft.Json.Converters
{
	internal class ExpandoObjectConverter : JsonConverter
	{
		internal override bool CanWrite
		{
			get
			{
				return false;
			}
		}
		internal override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
		}
		internal override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return this.ReadValue(reader);
		}
		private object ReadValue(JsonReader reader)
		{
			while (reader.TokenType == JsonToken.Comment)
			{
				if (!reader.Read())
				{
					throw JsonSerializationException.Create(reader, "Unexpected end when reading ExpandoObject.");
				}
			}
			switch (reader.TokenType)
			{
			case JsonToken.StartObject:
				return this.ReadObject(reader);
			case JsonToken.StartArray:
				return this.ReadList(reader);
			default:
				if (JsonReader.IsPrimitiveToken(reader.TokenType))
				{
					return reader.Value;
				}
				throw JsonSerializationException.Create(reader, "Unexpected token when converting ExpandoObject: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
			}
		}
		private object ReadList(JsonReader reader)
		{
			IList<object> list = new List<object>();
			while (reader.Read())
			{
				JsonToken tokenType = reader.TokenType;
				if (tokenType != JsonToken.Comment)
				{
					if (tokenType == JsonToken.EndArray)
					{
						return list;
					}
					object v = this.ReadValue(reader);
					list.Add(v);
				}
			}
			throw JsonSerializationException.Create(reader, "Unexpected end when reading ExpandoObject.");
		}
		private object ReadObject(JsonReader reader)
		{
			IDictionary<string, object> expandoObject = new ExpandoObject();
			while (reader.Read())
			{
				JsonToken tokenType = reader.TokenType;
				switch (tokenType)
				{
				case JsonToken.PropertyName:
				{
					string propertyName = reader.Value.ToString();
					if (!reader.Read())
					{
						throw JsonSerializationException.Create(reader, "Unexpected end when reading ExpandoObject.");
					}
					object v = this.ReadValue(reader);
					expandoObject[propertyName] = v;
					break;
				}
				case JsonToken.Comment:
					break;
				default:
					if (tokenType == JsonToken.EndObject)
					{
						return expandoObject;
					}
					break;
				}
			}
			throw JsonSerializationException.Create(reader, "Unexpected end when reading ExpandoObject.");
		}
		internal override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ExpandoObject);
		}
	}
}
