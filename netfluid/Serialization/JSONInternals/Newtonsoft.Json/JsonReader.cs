using Newtonsoft.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace Newtonsoft.Json
{
	internal abstract class JsonReader : IDisposable
	{
		protected internal enum State
		{
			Start,
			Complete,
			Property,
			ObjectStart,
			Object,
			ArrayStart,
			Array,
			Closed,
			PostValue,
			ConstructorStart,
			Constructor,
			Error,
			Finished
		}
		private JsonToken _tokenType;
		private object _value;
		internal char _quoteChar;
		internal JsonReader.State _currentState;
		internal ReadType _readType;
		private JsonPosition _currentPosition;
		private CultureInfo _culture;
		private DateTimeZoneHandling _dateTimeZoneHandling;
		private int? _maxDepth;
		private bool _hasExceededMaxDepth;
		internal DateParseHandling _dateParseHandling;
		internal FloatParseHandling _floatParseHandling;
		private string _dateFormatString;
		private readonly List<JsonPosition> _stack;
		protected JsonReader.State CurrentState
		{
			get
			{
				return this._currentState;
			}
		}
		internal bool CloseInput
		{
			get;
			set;
		}
		internal bool SupportMultipleContent
		{
			get;
			set;
		}
		internal virtual char QuoteChar
		{
			get
			{
				return this._quoteChar;
			}
			protected internal set
			{
				this._quoteChar = value;
			}
		}
		internal DateTimeZoneHandling DateTimeZoneHandling
		{
			get
			{
				return this._dateTimeZoneHandling;
			}
			set
			{
				this._dateTimeZoneHandling = value;
			}
		}
		internal DateParseHandling DateParseHandling
		{
			get
			{
				return this._dateParseHandling;
			}
			set
			{
				this._dateParseHandling = value;
			}
		}
		internal FloatParseHandling FloatParseHandling
		{
			get
			{
				return this._floatParseHandling;
			}
			set
			{
				this._floatParseHandling = value;
			}
		}
		internal string DateFormatString
		{
			get
			{
				return this._dateFormatString;
			}
			set
			{
				this._dateFormatString = value;
			}
		}
		internal int? MaxDepth
		{
			get
			{
				return this._maxDepth;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentException("Value must be positive.", "value");
				}
				this._maxDepth = value;
			}
		}
		internal virtual JsonToken TokenType
		{
			get
			{
				return this._tokenType;
			}
		}
		internal virtual object Value
		{
			get
			{
				return this._value;
			}
		}
		internal virtual Type ValueType
		{
			get
			{
				if (this._value == null)
				{
					return null;
				}
				return this._value.GetType();
			}
		}
		internal virtual int Depth
		{
			get
			{
				int depth = this._stack.Count;
				if (JsonReader.IsStartToken(this.TokenType) || this._currentPosition.Type == JsonContainerType.None)
				{
					return depth;
				}
				return depth + 1;
			}
		}
		internal virtual string Path
		{
			get
			{
				if (this._currentPosition.Type == JsonContainerType.None)
				{
					return string.Empty;
				}
				bool insideContainer = this._currentState != JsonReader.State.ArrayStart && this._currentState != JsonReader.State.ConstructorStart && this._currentState != JsonReader.State.ObjectStart;
				IEnumerable<JsonPosition> positions = (!insideContainer) ? this._stack : this._stack.Concat(new JsonPosition[]
				{
					this._currentPosition
				});
				return JsonPosition.BuildPath(positions);
			}
		}
		internal CultureInfo Culture
		{
			get
			{
				return this._culture ?? CultureInfo.InvariantCulture;
			}
			set
			{
				this._culture = value;
			}
		}
		internal JsonPosition GetPosition(int depth)
		{
			if (depth < this._stack.Count)
			{
				return this._stack[depth];
			}
			return this._currentPosition;
		}
		protected JsonReader()
		{
			this._currentState = JsonReader.State.Start;
			this._stack = new List<JsonPosition>(4);
			this._dateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
			this._dateParseHandling = DateParseHandling.DateTime;
			this._floatParseHandling = FloatParseHandling.Double;
			this.CloseInput = true;
		}
		private void Push(JsonContainerType value)
		{
			this.UpdateScopeWithFinishedValue();
			if (this._currentPosition.Type == JsonContainerType.None)
			{
				this._currentPosition = new JsonPosition(value);
				return;
			}
			this._stack.Add(this._currentPosition);
			this._currentPosition = new JsonPosition(value);
			if (this._maxDepth.HasValue && this.Depth + 1 > this._maxDepth && !this._hasExceededMaxDepth)
			{
				this._hasExceededMaxDepth = true;
				throw JsonReaderException.Create(this, "The reader's MaxDepth of {0} has been exceeded.".FormatWith(CultureInfo.InvariantCulture, this._maxDepth));
			}
		}
		private JsonContainerType Pop()
		{
			JsonPosition oldPosition;
			if (this._stack.Count > 0)
			{
				oldPosition = this._currentPosition;
				this._currentPosition = this._stack[this._stack.Count - 1];
				this._stack.RemoveAt(this._stack.Count - 1);
			}
			else
			{
				oldPosition = this._currentPosition;
				this._currentPosition = default(JsonPosition);
			}
			if (this._maxDepth.HasValue && this.Depth <= this._maxDepth)
			{
				this._hasExceededMaxDepth = false;
			}
			return oldPosition.Type;
		}
		private JsonContainerType Peek()
		{
			return this._currentPosition.Type;
		}
		internal abstract bool Read();
		internal abstract int? ReadAsInt32();
		internal abstract string ReadAsString();
		internal abstract byte[] ReadAsBytes();
		internal abstract decimal? ReadAsDecimal();
		internal abstract DateTime? ReadAsDateTime();
		internal abstract DateTimeOffset? ReadAsDateTimeOffset();
		internal virtual bool ReadInternal()
		{
			throw new NotImplementedException();
		}
		internal DateTimeOffset? ReadAsDateTimeOffsetInternal()
		{
			this._readType = ReadType.ReadAsDateTimeOffset;
			while (this.ReadInternal())
			{
				JsonToken t = this.TokenType;
				if (t != JsonToken.Comment)
				{
					if (t == JsonToken.Date)
					{
						if (this.Value is DateTime)
						{
							this.SetToken(JsonToken.Date, new DateTimeOffset((DateTime)this.Value), false);
						}
						return new DateTimeOffset?((DateTimeOffset)this.Value);
					}
					if (t == JsonToken.Null)
					{
						return null;
					}
					if (t == JsonToken.String)
					{
						string s = (string)this.Value;
						if (string.IsNullOrEmpty(s))
						{
							this.SetToken(JsonToken.Null);
							return null;
						}
						object temp;
						DateTimeOffset dt;
						if (DateTimeUtils.TryParseDateTime(s, DateParseHandling.DateTimeOffset, this.DateTimeZoneHandling, this._dateFormatString, this.Culture, out temp))
						{
							dt = (DateTimeOffset)temp;
							this.SetToken(JsonToken.Date, dt, false);
							return new DateTimeOffset?(dt);
						}
						if (DateTimeOffset.TryParse(s, this.Culture, DateTimeStyles.RoundtripKind, out dt))
						{
							this.SetToken(JsonToken.Date, dt, false);
							return new DateTimeOffset?(dt);
						}
						throw JsonReaderException.Create(this, "Could not convert string to DateTimeOffset: {0}.".FormatWith(CultureInfo.InvariantCulture, this.Value));
					}
					else
					{
						if (t == JsonToken.EndArray)
						{
							return null;
						}
						throw JsonReaderException.Create(this, "Error reading date. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, t));
					}
				}
			}
			this.SetToken(JsonToken.None);
			return null;
		}
		internal byte[] ReadAsBytesInternal()
		{
			this._readType = ReadType.ReadAsBytes;
			while (this.ReadInternal())
			{
				JsonToken t = this.TokenType;
				if (t != JsonToken.Comment)
				{
					if (this.IsWrappedInTypeObject())
					{
						byte[] data = this.ReadAsBytes();
						this.ReadInternal();
						this.SetToken(JsonToken.Bytes, data, false);
						return data;
					}
					if (t == JsonToken.String)
					{
						string s = (string)this.Value;
						byte[] data2;
						if (s.Length == 0)
						{
							data2 = new byte[0];
						}
						else
						{
							Guid g;
							if (ConvertUtils.TryConvertGuid(s, out g))
							{
								data2 = g.ToByteArray();
							}
							else
							{
								data2 = Convert.FromBase64String(s);
							}
						}
						this.SetToken(JsonToken.Bytes, data2, false);
						return data2;
					}
					if (t == JsonToken.Null)
					{
						return null;
					}
					if (t == JsonToken.Bytes)
					{
						if (this.ValueType == typeof(Guid))
						{
							byte[] data3 = ((Guid)this.Value).ToByteArray();
							this.SetToken(JsonToken.Bytes, data3, false);
							return data3;
						}
						return (byte[])this.Value;
					}
					else
					{
						if (t == JsonToken.StartArray)
						{
							List<byte> data4 = new List<byte>();
							while (this.ReadInternal())
							{
								t = this.TokenType;
								JsonToken jsonToken = t;
								switch (jsonToken)
								{
								case JsonToken.Comment:
									continue;
								case JsonToken.Raw:
									break;
								case JsonToken.Integer:
									data4.Add(Convert.ToByte(this.Value, CultureInfo.InvariantCulture));
									continue;
								default:
									if (jsonToken == JsonToken.EndArray)
									{
										byte[] d = data4.ToArray();
										this.SetToken(JsonToken.Bytes, d, false);
										return d;
									}
									break;
								}
								throw JsonReaderException.Create(this, "Unexpected token when reading bytes: {0}.".FormatWith(CultureInfo.InvariantCulture, t));
							}
							throw JsonReaderException.Create(this, "Unexpected end when reading bytes.");
						}
						if (t == JsonToken.EndArray)
						{
							return null;
						}
						throw JsonReaderException.Create(this, "Error reading bytes. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, t));
					}
				}
			}
			this.SetToken(JsonToken.None);
			return null;
		}
		internal decimal? ReadAsDecimalInternal()
		{
			this._readType = ReadType.ReadAsDecimal;
			while (this.ReadInternal())
			{
				JsonToken t = this.TokenType;
				if (t != JsonToken.Comment)
				{
					if (t == JsonToken.Integer || t == JsonToken.Float)
					{
						if (!(this.Value is decimal))
						{
							this.SetToken(JsonToken.Float, Convert.ToDecimal(this.Value, CultureInfo.InvariantCulture), false);
						}
						return new decimal?((decimal)this.Value);
					}
					if (t == JsonToken.Null)
					{
						return null;
					}
					if (t == JsonToken.String)
					{
						string s = (string)this.Value;
						if (string.IsNullOrEmpty(s))
						{
							this.SetToken(JsonToken.Null);
							return null;
						}
						decimal d;
						if (decimal.TryParse(s, NumberStyles.Number, this.Culture, out d))
						{
							this.SetToken(JsonToken.Float, d, false);
							return new decimal?(d);
						}
						throw JsonReaderException.Create(this, "Could not convert string to decimal: {0}.".FormatWith(CultureInfo.InvariantCulture, this.Value));
					}
					else
					{
						if (t == JsonToken.EndArray)
						{
							return null;
						}
						throw JsonReaderException.Create(this, "Error reading decimal. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, t));
					}
				}
			}
			this.SetToken(JsonToken.None);
			return null;
		}
		internal int? ReadAsInt32Internal()
		{
			this._readType = ReadType.ReadAsInt32;
			while (this.ReadInternal())
			{
				JsonToken t = this.TokenType;
				if (t != JsonToken.Comment)
				{
					if (t == JsonToken.Integer || t == JsonToken.Float)
					{
						if (!(this.Value is int))
						{
							this.SetToken(JsonToken.Integer, Convert.ToInt32(this.Value, CultureInfo.InvariantCulture), false);
						}
						return new int?((int)this.Value);
					}
					if (t == JsonToken.Null)
					{
						return null;
					}
					if (t == JsonToken.String)
					{
						string s = (string)this.Value;
						if (string.IsNullOrEmpty(s))
						{
							this.SetToken(JsonToken.Null);
							return null;
						}
						int i;
						if (int.TryParse(s, NumberStyles.Integer, this.Culture, out i))
						{
							this.SetToken(JsonToken.Integer, i, false);
							return new int?(i);
						}
						throw JsonReaderException.Create(this, "Could not convert string to integer: {0}.".FormatWith(CultureInfo.InvariantCulture, this.Value));
					}
					else
					{
						if (t == JsonToken.EndArray)
						{
							return null;
						}
						throw JsonReaderException.Create(this, "Error reading integer. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, this.TokenType));
					}
				}
			}
			this.SetToken(JsonToken.None);
			return null;
		}
		internal string ReadAsStringInternal()
		{
			this._readType = ReadType.ReadAsString;
			while (this.ReadInternal())
			{
				JsonToken t = this.TokenType;
				if (t != JsonToken.Comment)
				{
					if (t == JsonToken.String)
					{
						return (string)this.Value;
					}
					if (t == JsonToken.Null)
					{
						return null;
					}
					if (JsonReader.IsPrimitiveToken(t) && this.Value != null)
					{
						string s;
						if (this.Value is IFormattable)
						{
							s = ((IFormattable)this.Value).ToString(null, this.Culture);
						}
						else
						{
							s = this.Value.ToString();
						}
						this.SetToken(JsonToken.String, s, false);
						return s;
					}
					if (t == JsonToken.EndArray)
					{
						return null;
					}
					throw JsonReaderException.Create(this, "Error reading string. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, t));
				}
			}
			this.SetToken(JsonToken.None);
			return null;
		}
		internal DateTime? ReadAsDateTimeInternal()
		{
			this._readType = ReadType.ReadAsDateTime;
			while (this.ReadInternal())
			{
				if (this.TokenType != JsonToken.Comment)
				{
					if (this.TokenType == JsonToken.Date)
					{
						return new DateTime?((DateTime)this.Value);
					}
					if (this.TokenType == JsonToken.Null)
					{
						return null;
					}
					if (this.TokenType == JsonToken.String)
					{
						string s = (string)this.Value;
						if (string.IsNullOrEmpty(s))
						{
							this.SetToken(JsonToken.Null);
							return null;
						}
						object temp;
						DateTime dt;
						if (DateTimeUtils.TryParseDateTime(s, DateParseHandling.DateTime, this.DateTimeZoneHandling, this._dateFormatString, this.Culture, out temp))
						{
							dt = (DateTime)temp;
							dt = DateTimeUtils.EnsureDateTime(dt, this.DateTimeZoneHandling);
							this.SetToken(JsonToken.Date, dt, false);
							return new DateTime?(dt);
						}
						if (DateTime.TryParse(s, this.Culture, DateTimeStyles.RoundtripKind, out dt))
						{
							dt = DateTimeUtils.EnsureDateTime(dt, this.DateTimeZoneHandling);
							this.SetToken(JsonToken.Date, dt, false);
							return new DateTime?(dt);
						}
						throw JsonReaderException.Create(this, "Could not convert string to DateTime: {0}.".FormatWith(CultureInfo.InvariantCulture, this.Value));
					}
					else
					{
						if (this.TokenType == JsonToken.EndArray)
						{
							return null;
						}
						throw JsonReaderException.Create(this, "Error reading date. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, this.TokenType));
					}
				}
			}
			this.SetToken(JsonToken.None);
			return null;
		}
		private bool IsWrappedInTypeObject()
		{
			this._readType = ReadType.Read;
			if (this.TokenType != JsonToken.StartObject)
			{
				return false;
			}
			if (!this.ReadInternal())
			{
				throw JsonReaderException.Create(this, "Unexpected end when reading bytes.");
			}
			if (this.Value.ToString() == "$type")
			{
				this.ReadInternal();
				if (this.Value != null && this.Value.ToString().StartsWith("System.Byte[]", StringComparison.Ordinal))
				{
					this.ReadInternal();
					if (this.Value.ToString() == "$value")
					{
						return true;
					}
				}
			}
			throw JsonReaderException.Create(this, "Error reading bytes. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, JsonToken.StartObject));
		}
		internal void Skip()
		{
			if (this.TokenType == JsonToken.PropertyName)
			{
				this.Read();
			}
			if (JsonReader.IsStartToken(this.TokenType))
			{
				int depth = this.Depth;
				while (this.Read() && depth < this.Depth)
				{
				}
			}
		}
		protected void SetToken(JsonToken newToken)
		{
			this.SetToken(newToken, null, true);
		}
		protected void SetToken(JsonToken newToken, object value)
		{
			this.SetToken(newToken, value, true);
		}
		internal void SetToken(JsonToken newToken, object value, bool updateIndex)
		{
			this._tokenType = newToken;
			this._value = value;
			switch (newToken)
			{
			case JsonToken.StartObject:
				this._currentState = JsonReader.State.ObjectStart;
				this.Push(JsonContainerType.Object);
				return;
			case JsonToken.StartArray:
				this._currentState = JsonReader.State.ArrayStart;
				this.Push(JsonContainerType.Array);
				return;
			case JsonToken.StartConstructor:
				this._currentState = JsonReader.State.ConstructorStart;
				this.Push(JsonContainerType.Constructor);
				return;
			case JsonToken.PropertyName:
				this._currentState = JsonReader.State.Property;
				this._currentPosition.PropertyName = (string)value;
				return;
			case JsonToken.Comment:
				break;
			case JsonToken.Raw:
			case JsonToken.Integer:
			case JsonToken.Float:
			case JsonToken.String:
			case JsonToken.Boolean:
			case JsonToken.Null:
			case JsonToken.Undefined:
			case JsonToken.Date:
			case JsonToken.Bytes:
				this.SetPostValueState(updateIndex);
				break;
			case JsonToken.EndObject:
				this.ValidateEnd(JsonToken.EndObject);
				return;
			case JsonToken.EndArray:
				this.ValidateEnd(JsonToken.EndArray);
				return;
			case JsonToken.EndConstructor:
				this.ValidateEnd(JsonToken.EndConstructor);
				return;
			default:
				return;
			}
		}
		internal void SetPostValueState(bool updateIndex)
		{
			if (this.Peek() != JsonContainerType.None)
			{
				this._currentState = JsonReader.State.PostValue;
			}
			else
			{
				this.SetFinished();
			}
			if (updateIndex)
			{
				this.UpdateScopeWithFinishedValue();
			}
		}
		private void UpdateScopeWithFinishedValue()
		{
			if (this._currentPosition.HasIndex)
			{
				this._currentPosition.Position = this._currentPosition.Position + 1;
			}
		}
		private void ValidateEnd(JsonToken endToken)
		{
			JsonContainerType currentObject = this.Pop();
			if (this.GetTypeForCloseToken(endToken) != currentObject)
			{
				throw JsonReaderException.Create(this, "JsonToken {0} is not valid for closing JsonType {1}.".FormatWith(CultureInfo.InvariantCulture, endToken, currentObject));
			}
			if (this.Peek() != JsonContainerType.None)
			{
				this._currentState = JsonReader.State.PostValue;
				return;
			}
			this.SetFinished();
		}
		protected void SetStateBasedOnCurrent()
		{
			JsonContainerType currentObject = this.Peek();
			switch (currentObject)
			{
			case JsonContainerType.None:
				this.SetFinished();
				return;
			case JsonContainerType.Object:
				this._currentState = JsonReader.State.Object;
				return;
			case JsonContainerType.Array:
				this._currentState = JsonReader.State.Array;
				return;
			case JsonContainerType.Constructor:
				this._currentState = JsonReader.State.Constructor;
				return;
			default:
				throw JsonReaderException.Create(this, "While setting the reader state back to current object an unexpected JsonType was encountered: {0}".FormatWith(CultureInfo.InvariantCulture, currentObject));
			}
		}
		private void SetFinished()
		{
			if (this.SupportMultipleContent)
			{
				this._currentState = JsonReader.State.Start;
				return;
			}
			this._currentState = JsonReader.State.Finished;
		}
		internal static bool IsPrimitiveToken(JsonToken token)
		{
			switch (token)
			{
			case JsonToken.Integer:
			case JsonToken.Float:
			case JsonToken.String:
			case JsonToken.Boolean:
			case JsonToken.Null:
			case JsonToken.Undefined:
			case JsonToken.Date:
			case JsonToken.Bytes:
				return true;
			}
			return false;
		}
		internal static bool IsStartToken(JsonToken token)
		{
			switch (token)
			{
			case JsonToken.StartObject:
			case JsonToken.StartArray:
			case JsonToken.StartConstructor:
				return true;
			default:
				return false;
			}
		}
		private JsonContainerType GetTypeForCloseToken(JsonToken token)
		{
			switch (token)
			{
			case JsonToken.EndObject:
				return JsonContainerType.Object;
			case JsonToken.EndArray:
				return JsonContainerType.Array;
			case JsonToken.EndConstructor:
				return JsonContainerType.Constructor;
			default:
				throw JsonReaderException.Create(this, "Not a valid close JsonToken: {0}".FormatWith(CultureInfo.InvariantCulture, token));
			}
		}
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (this._currentState != JsonReader.State.Closed && disposing)
			{
				this.Close();
			}
		}
		internal virtual void Close()
		{
			this._currentState = JsonReader.State.Closed;
			this._tokenType = JsonToken.None;
			this._value = null;
		}
	}
}
