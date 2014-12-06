using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace Newtonsoft.Json
{
	internal class JsonValidatingReader : JsonReader, IJsonLineInfo
	{
		private class SchemaScope
		{
			private readonly JTokenType _tokenType;
			private readonly IList<JsonSchemaModel> _schemas;
			private readonly Dictionary<string, bool> _requiredProperties;
			internal string CurrentPropertyName
			{
				get;
				set;
			}
			internal int ArrayItemCount
			{
				get;
				set;
			}
			internal bool IsUniqueArray
			{
				get;
				set;
			}
			internal IList<JToken> UniqueArrayItems
			{
				get;
				set;
			}
			internal JTokenWriter CurrentItemWriter
			{
				get;
				set;
			}
			internal IList<JsonSchemaModel> Schemas
			{
				get
				{
					return this._schemas;
				}
			}
			internal Dictionary<string, bool> RequiredProperties
			{
				get
				{
					return this._requiredProperties;
				}
			}
			internal JTokenType TokenType
			{
				get
				{
					return this._tokenType;
				}
			}
			internal SchemaScope(JTokenType tokenType, IList<JsonSchemaModel> schemas)
			{
				this._tokenType = tokenType;
				this._schemas = schemas;
				this._requiredProperties = schemas.SelectMany(new Func<JsonSchemaModel, IEnumerable<string>>(this.GetRequiredProperties)).Distinct<string>().ToDictionary((string p) => p, (string p) => false);
				if (tokenType == JTokenType.Array)
				{
					if (schemas.Any((JsonSchemaModel s) => s.UniqueItems))
					{
						this.IsUniqueArray = true;
						this.UniqueArrayItems = new List<JToken>();
					}
				}
			}
			private IEnumerable<string> GetRequiredProperties(JsonSchemaModel schema)
			{
				if (schema == null || schema.Properties == null)
				{
					return Enumerable.Empty<string>();
				}
				return 
					from p in schema.Properties
					where p.Value.Required
					select p.Key;
			}
		}
		private readonly JsonReader _reader;
		private readonly Stack<JsonValidatingReader.SchemaScope> _stack;
		private JsonSchema _schema;
		private JsonSchemaModel _model;
		private JsonValidatingReader.SchemaScope _currentScope;
		private static readonly IList<JsonSchemaModel> EmptySchemaList = new List<JsonSchemaModel>();
		internal event ValidationEventHandler ValidationEventHandler;
		internal override object Value
		{
			get
			{
				return this._reader.Value;
			}
		}
		internal override int Depth
		{
			get
			{
				return this._reader.Depth;
			}
		}
		internal override string Path
		{
			get
			{
				return this._reader.Path;
			}
		}
		internal override char QuoteChar
		{
			get
			{
				return this._reader.QuoteChar;
			}
			protected internal set
			{
			}
		}
		internal override JsonToken TokenType
		{
			get
			{
				return this._reader.TokenType;
			}
		}
		internal override Type ValueType
		{
			get
			{
				return this._reader.ValueType;
			}
		}
		private IList<JsonSchemaModel> CurrentSchemas
		{
			get
			{
				return this._currentScope.Schemas;
			}
		}
		private IList<JsonSchemaModel> CurrentMemberSchemas
		{
			get
			{
				if (this._currentScope == null)
				{
					return new List<JsonSchemaModel>(new JsonSchemaModel[]
					{
						this._model
					});
				}
				if (this._currentScope.Schemas == null || this._currentScope.Schemas.Count == 0)
				{
					return JsonValidatingReader.EmptySchemaList;
				}
				switch (this._currentScope.TokenType)
				{
				case JTokenType.None:
					return this._currentScope.Schemas;
				case JTokenType.Object:
				{
					if (this._currentScope.CurrentPropertyName == null)
					{
						throw new JsonReaderException("CurrentPropertyName has not been set on scope.");
					}
					IList<JsonSchemaModel> schemas = new List<JsonSchemaModel>();
					foreach (JsonSchemaModel schema in this.CurrentSchemas)
					{
						JsonSchemaModel propertySchema;
						if (schema.Properties != null && schema.Properties.TryGetValue(this._currentScope.CurrentPropertyName, out propertySchema))
						{
							schemas.Add(propertySchema);
						}
						if (schema.PatternProperties != null)
						{
							foreach (KeyValuePair<string, JsonSchemaModel> patternProperty in schema.PatternProperties)
							{
								if (Regex.IsMatch(this._currentScope.CurrentPropertyName, patternProperty.Key))
								{
									schemas.Add(patternProperty.Value);
								}
							}
						}
						if (schemas.Count == 0 && schema.AllowAdditionalProperties && schema.AdditionalProperties != null)
						{
							schemas.Add(schema.AdditionalProperties);
						}
					}
					return schemas;
				}
				case JTokenType.Array:
				{
					IList<JsonSchemaModel> schemas2 = new List<JsonSchemaModel>();
					foreach (JsonSchemaModel schema2 in this.CurrentSchemas)
					{
						if (!schema2.PositionalItemsValidation)
						{
							if (schema2.Items != null && schema2.Items.Count > 0)
							{
								schemas2.Add(schema2.Items[0]);
							}
						}
						else
						{
							if (schema2.Items != null && schema2.Items.Count > 0 && schema2.Items.Count > this._currentScope.ArrayItemCount - 1)
							{
								schemas2.Add(schema2.Items[this._currentScope.ArrayItemCount - 1]);
							}
							if (schema2.AllowAdditionalItems && schema2.AdditionalItems != null)
							{
								schemas2.Add(schema2.AdditionalItems);
							}
						}
					}
					return schemas2;
				}
				case JTokenType.Constructor:
					return JsonValidatingReader.EmptySchemaList;
				default:
					throw new ArgumentOutOfRangeException("TokenType", "Unexpected token type: {0}".FormatWith(CultureInfo.InvariantCulture, this._currentScope.TokenType));
				}
			}
		}
		internal JsonSchema Schema
		{
			get
			{
				return this._schema;
			}
			set
			{
				if (this.TokenType != JsonToken.None)
				{
					throw new InvalidOperationException("Cannot change schema while validating JSON.");
				}
				this._schema = value;
				this._model = null;
			}
		}
		internal JsonReader Reader
		{
			get
			{
				return this._reader;
			}
		}
		int IJsonLineInfo.LineNumber
		{
			get
			{
				IJsonLineInfo lineInfo = this._reader as IJsonLineInfo;
				if (lineInfo == null)
				{
					return 0;
				}
				return lineInfo.LineNumber;
			}
		}
		int IJsonLineInfo.LinePosition
		{
			get
			{
				IJsonLineInfo lineInfo = this._reader as IJsonLineInfo;
				if (lineInfo == null)
				{
					return 0;
				}
				return lineInfo.LinePosition;
			}
		}
		private void Push(JsonValidatingReader.SchemaScope scope)
		{
			this._stack.Push(scope);
			this._currentScope = scope;
		}
		private JsonValidatingReader.SchemaScope Pop()
		{
			JsonValidatingReader.SchemaScope poppedScope = this._stack.Pop();
			this._currentScope = ((this._stack.Count != 0) ? this._stack.Peek() : null);
			return poppedScope;
		}
		private void RaiseError(string message, JsonSchemaModel schema)
		{
			string exceptionMessage = ((IJsonLineInfo)this).HasLineInfo() ? (message + " Line {0}, position {1}.".FormatWith(CultureInfo.InvariantCulture, ((IJsonLineInfo)this).LineNumber, ((IJsonLineInfo)this).LinePosition)) : message;
			this.OnValidationEvent(new JsonSchemaException(exceptionMessage, null, this.Path, ((IJsonLineInfo)this).LineNumber, ((IJsonLineInfo)this).LinePosition));
		}
		private void OnValidationEvent(JsonSchemaException exception)
		{
			ValidationEventHandler handler = this.ValidationEventHandler;
			if (handler != null)
			{
				handler(this, new ValidationEventArgs(exception));
				return;
			}
			throw exception;
		}
		internal JsonValidatingReader(JsonReader reader)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			this._reader = reader;
			this._stack = new Stack<JsonValidatingReader.SchemaScope>();
		}
		private void ValidateNotDisallowed(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			JsonSchemaType? currentNodeType = this.GetCurrentNodeSchemaType();
			if (currentNodeType.HasValue && JsonSchemaGenerator.HasFlag(new JsonSchemaType?(schema.Disallow), currentNodeType.Value))
			{
				this.RaiseError("Type {0} is disallowed.".FormatWith(CultureInfo.InvariantCulture, currentNodeType), schema);
			}
		}
		private JsonSchemaType? GetCurrentNodeSchemaType()
		{
			switch (this._reader.TokenType)
			{
			case JsonToken.StartObject:
				return new JsonSchemaType?(JsonSchemaType.Object);
			case JsonToken.StartArray:
				return new JsonSchemaType?(JsonSchemaType.Array);
			case JsonToken.Integer:
				return new JsonSchemaType?(JsonSchemaType.Integer);
			case JsonToken.Float:
				return new JsonSchemaType?(JsonSchemaType.Float);
			case JsonToken.String:
				return new JsonSchemaType?(JsonSchemaType.String);
			case JsonToken.Boolean:
				return new JsonSchemaType?(JsonSchemaType.Boolean);
			case JsonToken.Null:
				return new JsonSchemaType?(JsonSchemaType.Null);
			}
			return null;
		}
		internal override int? ReadAsInt32()
		{
			int? i = this._reader.ReadAsInt32();
			this.ValidateCurrentToken();
			return i;
		}
		internal override byte[] ReadAsBytes()
		{
			byte[] data = this._reader.ReadAsBytes();
			this.ValidateCurrentToken();
			return data;
		}
		internal override decimal? ReadAsDecimal()
		{
			decimal? d = this._reader.ReadAsDecimal();
			this.ValidateCurrentToken();
			return d;
		}
		internal override string ReadAsString()
		{
			string s = this._reader.ReadAsString();
			this.ValidateCurrentToken();
			return s;
		}
		internal override DateTime? ReadAsDateTime()
		{
			DateTime? dateTime = this._reader.ReadAsDateTime();
			this.ValidateCurrentToken();
			return dateTime;
		}
		internal override DateTimeOffset? ReadAsDateTimeOffset()
		{
			DateTimeOffset? dateTimeOffset = this._reader.ReadAsDateTimeOffset();
			this.ValidateCurrentToken();
			return dateTimeOffset;
		}
		internal override bool Read()
		{
			if (!this._reader.Read())
			{
				return false;
			}
			if (this._reader.TokenType == JsonToken.Comment)
			{
				return true;
			}
			this.ValidateCurrentToken();
			return true;
		}
		private void ValidateCurrentToken()
		{
			if (this._model == null)
			{
				JsonSchemaModelBuilder builder = new JsonSchemaModelBuilder();
				this._model = builder.Build(this._schema);
				if (!JsonWriter.IsStartToken(this._reader.TokenType))
				{
					this.Push(new JsonValidatingReader.SchemaScope(JTokenType.None, this.CurrentMemberSchemas));
				}
			}
			switch (this._reader.TokenType)
			{
			case JsonToken.None:
				return;
			case JsonToken.StartObject:
			{
				this.ProcessValue();
				IList<JsonSchemaModel> objectSchemas = this.CurrentMemberSchemas.Where(new Func<JsonSchemaModel, bool>(this.ValidateObject)).ToList<JsonSchemaModel>();
				this.Push(new JsonValidatingReader.SchemaScope(JTokenType.Object, objectSchemas));
				this.WriteToken(this.CurrentSchemas);
				return;
			}
			case JsonToken.StartArray:
			{
				this.ProcessValue();
				IList<JsonSchemaModel> arraySchemas = this.CurrentMemberSchemas.Where(new Func<JsonSchemaModel, bool>(this.ValidateArray)).ToList<JsonSchemaModel>();
				this.Push(new JsonValidatingReader.SchemaScope(JTokenType.Array, arraySchemas));
				this.WriteToken(this.CurrentSchemas);
				return;
			}
			case JsonToken.StartConstructor:
				this.ProcessValue();
				this.Push(new JsonValidatingReader.SchemaScope(JTokenType.Constructor, null));
				this.WriteToken(this.CurrentSchemas);
				return;
			case JsonToken.PropertyName:
				this.WriteToken(this.CurrentSchemas);
				using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentSchemas.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonSchemaModel schema = enumerator.Current;
						this.ValidatePropertyName(schema);
					}
					return;
				}
				break;
			case JsonToken.Comment:
				goto IL_3BD;
			case JsonToken.Raw:
				break;
			case JsonToken.Integer:
				this.ProcessValue();
				this.WriteToken(this.CurrentMemberSchemas);
				using (IEnumerator<JsonSchemaModel> enumerator2 = this.CurrentMemberSchemas.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						JsonSchemaModel schema2 = enumerator2.Current;
						this.ValidateInteger(schema2);
					}
					return;
				}
				goto IL_1D6;
			case JsonToken.Float:
				goto IL_1D6;
			case JsonToken.String:
				goto IL_222;
			case JsonToken.Boolean:
				goto IL_26E;
			case JsonToken.Null:
				goto IL_2BA;
			case JsonToken.Undefined:
			case JsonToken.Date:
			case JsonToken.Bytes:
				this.WriteToken(this.CurrentMemberSchemas);
				return;
			case JsonToken.EndObject:
				goto IL_306;
			case JsonToken.EndArray:
				this.WriteToken(this.CurrentSchemas);
				foreach (JsonSchemaModel schema3 in this.CurrentSchemas)
				{
					this.ValidateEndArray(schema3);
				}
				this.Pop();
				return;
			case JsonToken.EndConstructor:
				this.WriteToken(this.CurrentSchemas);
				this.Pop();
				return;
			default:
				goto IL_3BD;
			}
			this.ProcessValue();
			return;
			IL_1D6:
			this.ProcessValue();
			this.WriteToken(this.CurrentMemberSchemas);
			using (IEnumerator<JsonSchemaModel> enumerator4 = this.CurrentMemberSchemas.GetEnumerator())
			{
				while (enumerator4.MoveNext())
				{
					JsonSchemaModel schema4 = enumerator4.Current;
					this.ValidateFloat(schema4);
				}
				return;
			}
			IL_222:
			this.ProcessValue();
			this.WriteToken(this.CurrentMemberSchemas);
			using (IEnumerator<JsonSchemaModel> enumerator5 = this.CurrentMemberSchemas.GetEnumerator())
			{
				while (enumerator5.MoveNext())
				{
					JsonSchemaModel schema5 = enumerator5.Current;
					this.ValidateString(schema5);
				}
				return;
			}
			IL_26E:
			this.ProcessValue();
			this.WriteToken(this.CurrentMemberSchemas);
			using (IEnumerator<JsonSchemaModel> enumerator6 = this.CurrentMemberSchemas.GetEnumerator())
			{
				while (enumerator6.MoveNext())
				{
					JsonSchemaModel schema6 = enumerator6.Current;
					this.ValidateBoolean(schema6);
				}
				return;
			}
			IL_2BA:
			this.ProcessValue();
			this.WriteToken(this.CurrentMemberSchemas);
			using (IEnumerator<JsonSchemaModel> enumerator7 = this.CurrentMemberSchemas.GetEnumerator())
			{
				while (enumerator7.MoveNext())
				{
					JsonSchemaModel schema7 = enumerator7.Current;
					this.ValidateNull(schema7);
				}
				return;
			}
			IL_306:
			this.WriteToken(this.CurrentSchemas);
			foreach (JsonSchemaModel schema8 in this.CurrentSchemas)
			{
				this.ValidateEndObject(schema8);
			}
			this.Pop();
			return;
			IL_3BD:
			throw new ArgumentOutOfRangeException();
		}
		private void WriteToken(IList<JsonSchemaModel> schemas)
		{
			foreach (JsonValidatingReader.SchemaScope schemaScope in this._stack)
			{
				bool isInUniqueArray = schemaScope.TokenType == JTokenType.Array && schemaScope.IsUniqueArray && schemaScope.ArrayItemCount > 0;
				if (!isInUniqueArray)
				{
					if (!schemas.Any((JsonSchemaModel s) => s.Enum != null))
					{
						continue;
					}
				}
				if (schemaScope.CurrentItemWriter == null)
				{
					if (JsonWriter.IsEndToken(this._reader.TokenType))
					{
						continue;
					}
					schemaScope.CurrentItemWriter = new JTokenWriter();
				}
				schemaScope.CurrentItemWriter.WriteToken(this._reader, false);
				if (schemaScope.CurrentItemWriter.Top == 0 && this._reader.TokenType != JsonToken.PropertyName)
				{
					JToken finishedItem = schemaScope.CurrentItemWriter.Token;
					schemaScope.CurrentItemWriter = null;
					if (isInUniqueArray)
					{
						if (schemaScope.UniqueArrayItems.Contains(finishedItem, JToken.EqualityComparer))
						{
							this.RaiseError("Non-unique array item at index {0}.".FormatWith(CultureInfo.InvariantCulture, schemaScope.ArrayItemCount - 1), schemaScope.Schemas.First((JsonSchemaModel s) => s.UniqueItems));
						}
						schemaScope.UniqueArrayItems.Add(finishedItem);
					}
					else
					{
						if (schemas.Any((JsonSchemaModel s) => s.Enum != null))
						{
							foreach (JsonSchemaModel schema in schemas)
							{
								if (schema.Enum != null && !schema.Enum.ContainsValue(finishedItem, JToken.EqualityComparer))
								{
									StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
									finishedItem.WriteTo(new JsonTextWriter(sw), new JsonConverter[0]);
									this.RaiseError("Value {0} is not defined in enum.".FormatWith(CultureInfo.InvariantCulture, sw.ToString()), schema);
								}
							}
						}
					}
				}
			}
		}
		private void ValidateEndObject(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			Dictionary<string, bool> requiredProperties = this._currentScope.RequiredProperties;
			if (requiredProperties != null)
			{
				List<string> unmatchedRequiredProperties = (
					from kv in requiredProperties
					where !kv.Value
					select kv.Key).ToList<string>();
				if (unmatchedRequiredProperties.Count > 0)
				{
					this.RaiseError("Required properties are missing from object: {0}.".FormatWith(CultureInfo.InvariantCulture, string.Join(", ", unmatchedRequiredProperties.ToArray())), schema);
				}
			}
		}
		private void ValidateEndArray(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			int arrayItemCount = this._currentScope.ArrayItemCount;
			if (schema.MaximumItems.HasValue && arrayItemCount > schema.MaximumItems)
			{
				this.RaiseError("Array item count {0} exceeds maximum count of {1}.".FormatWith(CultureInfo.InvariantCulture, arrayItemCount, schema.MaximumItems), schema);
			}
			if (schema.MinimumItems.HasValue && arrayItemCount < schema.MinimumItems)
			{
				this.RaiseError("Array item count {0} is less than minimum count of {1}.".FormatWith(CultureInfo.InvariantCulture, arrayItemCount, schema.MinimumItems), schema);
			}
		}
		private void ValidateNull(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			if (!this.TestType(schema, JsonSchemaType.Null))
			{
				return;
			}
			this.ValidateNotDisallowed(schema);
		}
		private void ValidateBoolean(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			if (!this.TestType(schema, JsonSchemaType.Boolean))
			{
				return;
			}
			this.ValidateNotDisallowed(schema);
		}
		private void ValidateString(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			if (!this.TestType(schema, JsonSchemaType.String))
			{
				return;
			}
			this.ValidateNotDisallowed(schema);
			string value = this._reader.Value.ToString();
			if (schema.MaximumLength.HasValue && value.Length > schema.MaximumLength)
			{
				this.RaiseError("String '{0}' exceeds maximum length of {1}.".FormatWith(CultureInfo.InvariantCulture, value, schema.MaximumLength), schema);
			}
			if (schema.MinimumLength.HasValue && value.Length < schema.MinimumLength)
			{
				this.RaiseError("String '{0}' is less than minimum length of {1}.".FormatWith(CultureInfo.InvariantCulture, value, schema.MinimumLength), schema);
			}
			if (schema.Patterns != null)
			{
				foreach (string pattern in schema.Patterns)
				{
					if (!Regex.IsMatch(value, pattern))
					{
						this.RaiseError("String '{0}' does not match regex pattern '{1}'.".FormatWith(CultureInfo.InvariantCulture, value, pattern), schema);
					}
				}
			}
		}
		private void ValidateInteger(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			if (!this.TestType(schema, JsonSchemaType.Integer))
			{
				return;
			}
			this.ValidateNotDisallowed(schema);
			object value = this._reader.Value;
			if (schema.Maximum.HasValue)
			{
				if (JValue.Compare(JTokenType.Integer, value, schema.Maximum) > 0)
				{
					this.RaiseError("Integer {0} exceeds maximum value of {1}.".FormatWith(CultureInfo.InvariantCulture, value, schema.Maximum), schema);
				}
				if (schema.ExclusiveMaximum && JValue.Compare(JTokenType.Integer, value, schema.Maximum) == 0)
				{
					this.RaiseError("Integer {0} equals maximum value of {1} and exclusive maximum is true.".FormatWith(CultureInfo.InvariantCulture, value, schema.Maximum), schema);
				}
			}
			if (schema.Minimum.HasValue)
			{
				if (JValue.Compare(JTokenType.Integer, value, schema.Minimum) < 0)
				{
					this.RaiseError("Integer {0} is less than minimum value of {1}.".FormatWith(CultureInfo.InvariantCulture, value, schema.Minimum), schema);
				}
				if (schema.ExclusiveMinimum && JValue.Compare(JTokenType.Integer, value, schema.Minimum) == 0)
				{
					this.RaiseError("Integer {0} equals minimum value of {1} and exclusive minimum is true.".FormatWith(CultureInfo.InvariantCulture, value, schema.Minimum), schema);
				}
			}
			if (schema.DivisibleBy.HasValue)
			{
				bool notDivisible = !JsonValidatingReader.IsZero((double)Convert.ToInt64(value, CultureInfo.InvariantCulture) % schema.DivisibleBy.Value);
				if (notDivisible)
				{
					this.RaiseError("Integer {0} is not evenly divisible by {1}.".FormatWith(CultureInfo.InvariantCulture, JsonConvert.ToString(value), schema.DivisibleBy), schema);
				}
			}
		}
		private void ProcessValue()
		{
			if (this._currentScope != null && this._currentScope.TokenType == JTokenType.Array)
			{
				this._currentScope.ArrayItemCount++;
				foreach (JsonSchemaModel currentSchema in this.CurrentSchemas)
				{
					if (currentSchema != null && currentSchema.PositionalItemsValidation && !currentSchema.AllowAdditionalItems && (currentSchema.Items == null || this._currentScope.ArrayItemCount - 1 >= currentSchema.Items.Count))
					{
						this.RaiseError("Index {0} has not been defined and the schema does not allow additional items.".FormatWith(CultureInfo.InvariantCulture, this._currentScope.ArrayItemCount), currentSchema);
					}
				}
			}
		}
		private void ValidateFloat(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			if (!this.TestType(schema, JsonSchemaType.Float))
			{
				return;
			}
			this.ValidateNotDisallowed(schema);
			double value = Convert.ToDouble(this._reader.Value, CultureInfo.InvariantCulture);
			if (schema.Maximum.HasValue)
			{
				double num = value;
				double? maximum = schema.Maximum;
				if (num > maximum.GetValueOrDefault() && maximum.HasValue)
				{
					this.RaiseError("Float {0} exceeds maximum value of {1}.".FormatWith(CultureInfo.InvariantCulture, JsonConvert.ToString(value), schema.Maximum), schema);
				}
				if (schema.ExclusiveMaximum)
				{
					double num2 = value;
					double? maximum2 = schema.Maximum;
					if (num2 == maximum2.GetValueOrDefault() && maximum2.HasValue)
					{
						this.RaiseError("Float {0} equals maximum value of {1} and exclusive maximum is true.".FormatWith(CultureInfo.InvariantCulture, JsonConvert.ToString(value), schema.Maximum), schema);
					}
				}
			}
			if (schema.Minimum.HasValue)
			{
				double num3 = value;
				double? minimum = schema.Minimum;
				if (num3 < minimum.GetValueOrDefault() && minimum.HasValue)
				{
					this.RaiseError("Float {0} is less than minimum value of {1}.".FormatWith(CultureInfo.InvariantCulture, JsonConvert.ToString(value), schema.Minimum), schema);
				}
				if (schema.ExclusiveMinimum)
				{
					double num4 = value;
					double? minimum2 = schema.Minimum;
					if (num4 == minimum2.GetValueOrDefault() && minimum2.HasValue)
					{
						this.RaiseError("Float {0} equals minimum value of {1} and exclusive minimum is true.".FormatWith(CultureInfo.InvariantCulture, JsonConvert.ToString(value), schema.Minimum), schema);
					}
				}
			}
			if (schema.DivisibleBy.HasValue)
			{
				double remainder = JsonValidatingReader.FloatingPointRemainder(value, schema.DivisibleBy.Value);
				if (!JsonValidatingReader.IsZero(remainder))
				{
					this.RaiseError("Float {0} is not evenly divisible by {1}.".FormatWith(CultureInfo.InvariantCulture, JsonConvert.ToString(value), schema.DivisibleBy), schema);
				}
			}
		}
		private static double FloatingPointRemainder(double dividend, double divisor)
		{
			return dividend - Math.Floor(dividend / divisor) * divisor;
		}
		private static bool IsZero(double value)
		{
			return Math.Abs(value) < 4.4408920985006262E-15;
		}
		private void ValidatePropertyName(JsonSchemaModel schema)
		{
			if (schema == null)
			{
				return;
			}
			string propertyName = Convert.ToString(this._reader.Value, CultureInfo.InvariantCulture);
			if (this._currentScope.RequiredProperties.ContainsKey(propertyName))
			{
				this._currentScope.RequiredProperties[propertyName] = true;
			}
			if (!schema.AllowAdditionalProperties && !this.IsPropertyDefinied(schema, propertyName))
			{
				this.RaiseError("Property '{0}' has not been defined and the schema does not allow additional properties.".FormatWith(CultureInfo.InvariantCulture, propertyName), schema);
			}
			this._currentScope.CurrentPropertyName = propertyName;
		}
		private bool IsPropertyDefinied(JsonSchemaModel schema, string propertyName)
		{
			if (schema.Properties != null && schema.Properties.ContainsKey(propertyName))
			{
				return true;
			}
			if (schema.PatternProperties != null)
			{
				foreach (string pattern in schema.PatternProperties.Keys)
				{
					if (Regex.IsMatch(propertyName, pattern))
					{
						return true;
					}
				}
				return false;
			}
			return false;
		}
		private bool ValidateArray(JsonSchemaModel schema)
		{
			return schema == null || this.TestType(schema, JsonSchemaType.Array);
		}
		private bool ValidateObject(JsonSchemaModel schema)
		{
			return schema == null || this.TestType(schema, JsonSchemaType.Object);
		}
		private bool TestType(JsonSchemaModel currentSchema, JsonSchemaType currentType)
		{
			if (!JsonSchemaGenerator.HasFlag(new JsonSchemaType?(currentSchema.Type), currentType))
			{
				this.RaiseError("Invalid type. Expected {0} but got {1}.".FormatWith(CultureInfo.InvariantCulture, currentSchema.Type, currentType), currentSchema);
				return false;
			}
			return true;
		}
		bool IJsonLineInfo.HasLineInfo()
		{
			IJsonLineInfo lineInfo = this._reader as IJsonLineInfo;
			return lineInfo != null && lineInfo.HasLineInfo();
		}
	}
}
