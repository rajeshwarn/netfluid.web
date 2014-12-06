using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
namespace Newtonsoft.Json
{
	internal class JsonSerializer
	{
		internal TypeNameHandling _typeNameHandling;
		internal FormatterAssemblyStyle _typeNameAssemblyFormat;
		internal PreserveReferencesHandling _preserveReferencesHandling;
		internal ReferenceLoopHandling _referenceLoopHandling;
		internal MissingMemberHandling _missingMemberHandling;
		internal ObjectCreationHandling _objectCreationHandling;
		internal NullValueHandling _nullValueHandling;
		internal DefaultValueHandling _defaultValueHandling;
		internal ConstructorHandling _constructorHandling;
		internal MetadataPropertyHandling _metadataPropertyHandling;
		internal JsonConverterCollection _converters;
		internal IContractResolver _contractResolver;
		internal ITraceWriter _traceWriter;
		internal SerializationBinder _binder;
		internal StreamingContext _context;
		private IReferenceResolver _referenceResolver;
		private Formatting? _formatting;
		private DateFormatHandling? _dateFormatHandling;
		private DateTimeZoneHandling? _dateTimeZoneHandling;
		private DateParseHandling? _dateParseHandling;
		private FloatFormatHandling? _floatFormatHandling;
		private FloatParseHandling? _floatParseHandling;
		private StringEscapeHandling? _stringEscapeHandling;
		private CultureInfo _culture;
		private int? _maxDepth;
		private bool _maxDepthSet;
		private bool? _checkAdditionalContent;
		private string _dateFormatString;
		private bool _dateFormatStringSet;
		internal virtual event EventHandler<ErrorEventArgs> Error;
		internal virtual IReferenceResolver ReferenceResolver
		{
			get
			{
				return this.GetReferenceResolver();
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "Reference resolver cannot be null.");
				}
				this._referenceResolver = value;
			}
		}
		internal virtual SerializationBinder Binder
		{
			get
			{
				return this._binder;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "Serialization binder cannot be null.");
				}
				this._binder = value;
			}
		}
		internal virtual ITraceWriter TraceWriter
		{
			get
			{
				return this._traceWriter;
			}
			set
			{
				this._traceWriter = value;
			}
		}
		internal virtual TypeNameHandling TypeNameHandling
		{
			get
			{
				return this._typeNameHandling;
			}
			set
			{
				if (value < TypeNameHandling.None || value > TypeNameHandling.Auto)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._typeNameHandling = value;
			}
		}
		internal virtual FormatterAssemblyStyle TypeNameAssemblyFormat
		{
			get
			{
				return this._typeNameAssemblyFormat;
			}
			set
			{
				if (value < FormatterAssemblyStyle.Simple || value > FormatterAssemblyStyle.Full)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._typeNameAssemblyFormat = value;
			}
		}
		internal virtual PreserveReferencesHandling PreserveReferencesHandling
		{
			get
			{
				return this._preserveReferencesHandling;
			}
			set
			{
				if (value < PreserveReferencesHandling.None || value > PreserveReferencesHandling.All)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._preserveReferencesHandling = value;
			}
		}
		internal virtual ReferenceLoopHandling ReferenceLoopHandling
		{
			get
			{
				return this._referenceLoopHandling;
			}
			set
			{
				if (value < ReferenceLoopHandling.Error || value > ReferenceLoopHandling.Serialize)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._referenceLoopHandling = value;
			}
		}
		internal virtual MissingMemberHandling MissingMemberHandling
		{
			get
			{
				return this._missingMemberHandling;
			}
			set
			{
				if (value < MissingMemberHandling.Ignore || value > MissingMemberHandling.Error)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._missingMemberHandling = value;
			}
		}
		internal virtual NullValueHandling NullValueHandling
		{
			get
			{
				return this._nullValueHandling;
			}
			set
			{
				if (value < NullValueHandling.Include || value > NullValueHandling.Ignore)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._nullValueHandling = value;
			}
		}
		internal virtual DefaultValueHandling DefaultValueHandling
		{
			get
			{
				return this._defaultValueHandling;
			}
			set
			{
				if (value < DefaultValueHandling.Include || value > DefaultValueHandling.IgnoreAndPopulate)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._defaultValueHandling = value;
			}
		}
		internal virtual ObjectCreationHandling ObjectCreationHandling
		{
			get
			{
				return this._objectCreationHandling;
			}
			set
			{
				if (value < ObjectCreationHandling.Auto || value > ObjectCreationHandling.Replace)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._objectCreationHandling = value;
			}
		}
		internal virtual ConstructorHandling ConstructorHandling
		{
			get
			{
				return this._constructorHandling;
			}
			set
			{
				if (value < ConstructorHandling.Default || value > ConstructorHandling.AllowNonPublicDefaultConstructor)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._constructorHandling = value;
			}
		}
		internal virtual MetadataPropertyHandling MetadataPropertyHandling
		{
			get
			{
				return this._metadataPropertyHandling;
			}
			set
			{
				if (value < MetadataPropertyHandling.Default || value > MetadataPropertyHandling.Ignore)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._metadataPropertyHandling = value;
			}
		}
		internal virtual JsonConverterCollection Converters
		{
			get
			{
				if (this._converters == null)
				{
					this._converters = new JsonConverterCollection();
				}
				return this._converters;
			}
		}
		internal virtual IContractResolver ContractResolver
		{
			get
			{
				return this._contractResolver;
			}
			set
			{
				this._contractResolver = (value ?? DefaultContractResolver.Instance);
			}
		}
		internal virtual StreamingContext Context
		{
			get
			{
				return this._context;
			}
			set
			{
				this._context = value;
			}
		}
		internal virtual Formatting Formatting
		{
			get
			{
				Formatting? formatting = this._formatting;
				if (!formatting.HasValue)
				{
					return Formatting.None;
				}
				return formatting.GetValueOrDefault();
			}
			set
			{
				this._formatting = new Formatting?(value);
			}
		}
		internal virtual DateFormatHandling DateFormatHandling
		{
			get
			{
				DateFormatHandling? dateFormatHandling = this._dateFormatHandling;
				if (!dateFormatHandling.HasValue)
				{
					return DateFormatHandling.IsoDateFormat;
				}
				return dateFormatHandling.GetValueOrDefault();
			}
			set
			{
				this._dateFormatHandling = new DateFormatHandling?(value);
			}
		}
		internal virtual DateTimeZoneHandling DateTimeZoneHandling
		{
			get
			{
				DateTimeZoneHandling? dateTimeZoneHandling = this._dateTimeZoneHandling;
				if (!dateTimeZoneHandling.HasValue)
				{
					return DateTimeZoneHandling.RoundtripKind;
				}
				return dateTimeZoneHandling.GetValueOrDefault();
			}
			set
			{
				this._dateTimeZoneHandling = new DateTimeZoneHandling?(value);
			}
		}
		internal virtual DateParseHandling DateParseHandling
		{
			get
			{
				DateParseHandling? dateParseHandling = this._dateParseHandling;
				if (!dateParseHandling.HasValue)
				{
					return DateParseHandling.DateTime;
				}
				return dateParseHandling.GetValueOrDefault();
			}
			set
			{
				this._dateParseHandling = new DateParseHandling?(value);
			}
		}
		internal virtual FloatParseHandling FloatParseHandling
		{
			get
			{
				FloatParseHandling? floatParseHandling = this._floatParseHandling;
				if (!floatParseHandling.HasValue)
				{
					return FloatParseHandling.Double;
				}
				return floatParseHandling.GetValueOrDefault();
			}
			set
			{
				this._floatParseHandling = new FloatParseHandling?(value);
			}
		}
		internal virtual FloatFormatHandling FloatFormatHandling
		{
			get
			{
				FloatFormatHandling? floatFormatHandling = this._floatFormatHandling;
				if (!floatFormatHandling.HasValue)
				{
					return FloatFormatHandling.String;
				}
				return floatFormatHandling.GetValueOrDefault();
			}
			set
			{
				this._floatFormatHandling = new FloatFormatHandling?(value);
			}
		}
		internal virtual StringEscapeHandling StringEscapeHandling
		{
			get
			{
				StringEscapeHandling? stringEscapeHandling = this._stringEscapeHandling;
				if (!stringEscapeHandling.HasValue)
				{
					return StringEscapeHandling.Default;
				}
				return stringEscapeHandling.GetValueOrDefault();
			}
			set
			{
				this._stringEscapeHandling = new StringEscapeHandling?(value);
			}
		}
		internal virtual string DateFormatString
		{
			get
			{
				return this._dateFormatString ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
			}
			set
			{
				this._dateFormatString = value;
				this._dateFormatStringSet = true;
			}
		}
		internal virtual CultureInfo Culture
		{
			get
			{
				return this._culture ?? JsonSerializerSettings.DefaultCulture;
			}
			set
			{
				this._culture = value;
			}
		}
		internal virtual int? MaxDepth
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
				this._maxDepthSet = true;
			}
		}
		internal virtual bool CheckAdditionalContent
		{
			get
			{
				return this._checkAdditionalContent ?? false;
			}
			set
			{
				this._checkAdditionalContent = new bool?(value);
			}
		}
		internal bool IsCheckAdditionalContentSet()
		{
			return this._checkAdditionalContent.HasValue;
		}
		internal JsonSerializer()
		{
			this._referenceLoopHandling = ReferenceLoopHandling.Error;
			this._missingMemberHandling = MissingMemberHandling.Ignore;
			this._nullValueHandling = NullValueHandling.Include;
			this._defaultValueHandling = DefaultValueHandling.Include;
			this._objectCreationHandling = ObjectCreationHandling.Auto;
			this._preserveReferencesHandling = PreserveReferencesHandling.None;
			this._constructorHandling = ConstructorHandling.Default;
			this._typeNameHandling = TypeNameHandling.None;
			this._metadataPropertyHandling = MetadataPropertyHandling.Default;
			this._context = JsonSerializerSettings.DefaultContext;
			this._binder = DefaultSerializationBinder.Instance;
			this._culture = JsonSerializerSettings.DefaultCulture;
			this._contractResolver = DefaultContractResolver.Instance;
		}
		internal static JsonSerializer Create()
		{
			return new JsonSerializer();
		}
		internal static JsonSerializer Create(JsonSerializerSettings settings)
		{
			JsonSerializer serializer = JsonSerializer.Create();
			if (settings != null)
			{
				JsonSerializer.ApplySerializerSettings(serializer, settings);
			}
			return serializer;
		}
		internal static JsonSerializer CreateDefault()
		{
			Func<JsonSerializerSettings> defaultSettingsCreator = JsonConvert.DefaultSettings;
			JsonSerializerSettings defaultSettings = (defaultSettingsCreator != null) ? defaultSettingsCreator() : null;
			return JsonSerializer.Create(defaultSettings);
		}
		internal static JsonSerializer CreateDefault(JsonSerializerSettings settings)
		{
			JsonSerializer serializer = JsonSerializer.CreateDefault();
			if (settings != null)
			{
				JsonSerializer.ApplySerializerSettings(serializer, settings);
			}
			return serializer;
		}
		private static void ApplySerializerSettings(JsonSerializer serializer, JsonSerializerSettings settings)
		{
			if (!CollectionUtils.IsNullOrEmpty<JsonConverter>(settings.Converters))
			{
				for (int i = 0; i < settings.Converters.Count; i++)
				{
					serializer.Converters.Insert(i, settings.Converters[i]);
				}
			}
			if (settings._typeNameHandling.HasValue)
			{
				serializer.TypeNameHandling = settings.TypeNameHandling;
			}
			if (settings._metadataPropertyHandling.HasValue)
			{
				serializer.MetadataPropertyHandling = settings.MetadataPropertyHandling;
			}
			if (settings._typeNameAssemblyFormat.HasValue)
			{
				serializer.TypeNameAssemblyFormat = settings.TypeNameAssemblyFormat;
			}
			if (settings._preserveReferencesHandling.HasValue)
			{
				serializer.PreserveReferencesHandling = settings.PreserveReferencesHandling;
			}
			if (settings._referenceLoopHandling.HasValue)
			{
				serializer.ReferenceLoopHandling = settings.ReferenceLoopHandling;
			}
			if (settings._missingMemberHandling.HasValue)
			{
				serializer.MissingMemberHandling = settings.MissingMemberHandling;
			}
			if (settings._objectCreationHandling.HasValue)
			{
				serializer.ObjectCreationHandling = settings.ObjectCreationHandling;
			}
			if (settings._nullValueHandling.HasValue)
			{
				serializer.NullValueHandling = settings.NullValueHandling;
			}
			if (settings._defaultValueHandling.HasValue)
			{
				serializer.DefaultValueHandling = settings.DefaultValueHandling;
			}
			if (settings._constructorHandling.HasValue)
			{
				serializer.ConstructorHandling = settings.ConstructorHandling;
			}
			if (settings._context.HasValue)
			{
				serializer.Context = settings.Context;
			}
			if (settings._checkAdditionalContent.HasValue)
			{
				serializer._checkAdditionalContent = settings._checkAdditionalContent;
			}
			if (settings.Error != null)
			{
				serializer.Error += settings.Error;
			}
			if (settings.ContractResolver != null)
			{
				serializer.ContractResolver = settings.ContractResolver;
			}
			if (settings.ReferenceResolver != null)
			{
				serializer.ReferenceResolver = settings.ReferenceResolver;
			}
			if (settings.TraceWriter != null)
			{
				serializer.TraceWriter = settings.TraceWriter;
			}
			if (settings.Binder != null)
			{
				serializer.Binder = settings.Binder;
			}
			if (settings._formatting.HasValue)
			{
				serializer._formatting = settings._formatting;
			}
			if (settings._dateFormatHandling.HasValue)
			{
				serializer._dateFormatHandling = settings._dateFormatHandling;
			}
			if (settings._dateTimeZoneHandling.HasValue)
			{
				serializer._dateTimeZoneHandling = settings._dateTimeZoneHandling;
			}
			if (settings._dateParseHandling.HasValue)
			{
				serializer._dateParseHandling = settings._dateParseHandling;
			}
			if (settings._dateFormatStringSet)
			{
				serializer._dateFormatString = settings._dateFormatString;
				serializer._dateFormatStringSet = settings._dateFormatStringSet;
			}
			if (settings._floatFormatHandling.HasValue)
			{
				serializer._floatFormatHandling = settings._floatFormatHandling;
			}
			if (settings._floatParseHandling.HasValue)
			{
				serializer._floatParseHandling = settings._floatParseHandling;
			}
			if (settings._stringEscapeHandling.HasValue)
			{
				serializer._stringEscapeHandling = settings._stringEscapeHandling;
			}
			if (settings._culture != null)
			{
				serializer._culture = settings._culture;
			}
			if (settings._maxDepthSet)
			{
				serializer._maxDepth = settings._maxDepth;
				serializer._maxDepthSet = settings._maxDepthSet;
			}
		}
		internal void Populate(TextReader reader, object target)
		{
			this.Populate(new JsonTextReader(reader), target);
		}
		internal void Populate(JsonReader reader, object target)
		{
			this.PopulateInternal(reader, target);
		}
		internal virtual void PopulateInternal(JsonReader reader, object target)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			ValidationUtils.ArgumentNotNull(target, "target");
			CultureInfo previousCulture;
			DateTimeZoneHandling? previousDateTimeZoneHandling;
			DateParseHandling? previousDateParseHandling;
			FloatParseHandling? previousFloatParseHandling;
			int? previousMaxDepth;
			string previousDateFormatString;
			this.SetupReader(reader, out previousCulture, out previousDateTimeZoneHandling, out previousDateParseHandling, out previousFloatParseHandling, out previousMaxDepth, out previousDateFormatString);
			TraceJsonReader traceJsonReader = (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose) ? new TraceJsonReader(reader) : null;
			JsonSerializerInternalReader serializerReader = new JsonSerializerInternalReader(this);
			serializerReader.Populate(traceJsonReader ?? reader, target);
			if (traceJsonReader != null)
			{
				this.TraceWriter.Trace(TraceLevel.Verbose, "Deserialized JSON: " + Environment.NewLine + traceJsonReader.GetJson(), null);
			}
			this.ResetReader(reader, previousCulture, previousDateTimeZoneHandling, previousDateParseHandling, previousFloatParseHandling, previousMaxDepth, previousDateFormatString);
		}
		internal object Deserialize(JsonReader reader)
		{
			return this.Deserialize(reader, null);
		}
		internal object Deserialize(TextReader reader, Type objectType)
		{
			return this.Deserialize(new JsonTextReader(reader), objectType);
		}
		internal T Deserialize<T>(JsonReader reader)
		{
			return (T)((object)this.Deserialize(reader, typeof(T)));
		}
		internal object Deserialize(JsonReader reader, Type objectType)
		{
			return this.DeserializeInternal(reader, objectType);
		}
		internal virtual object DeserializeInternal(JsonReader reader, Type objectType)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			CultureInfo previousCulture;
			DateTimeZoneHandling? previousDateTimeZoneHandling;
			DateParseHandling? previousDateParseHandling;
			FloatParseHandling? previousFloatParseHandling;
			int? previousMaxDepth;
			string previousDateFormatString;
			this.SetupReader(reader, out previousCulture, out previousDateTimeZoneHandling, out previousDateParseHandling, out previousFloatParseHandling, out previousMaxDepth, out previousDateFormatString);
			TraceJsonReader traceJsonReader = (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose) ? new TraceJsonReader(reader) : null;
			JsonSerializerInternalReader serializerReader = new JsonSerializerInternalReader(this);
			object value = serializerReader.Deserialize(traceJsonReader ?? reader, objectType, this.CheckAdditionalContent);
			if (traceJsonReader != null)
			{
				this.TraceWriter.Trace(TraceLevel.Verbose, "Deserialized JSON: " + Environment.NewLine + traceJsonReader.GetJson(), null);
			}
			this.ResetReader(reader, previousCulture, previousDateTimeZoneHandling, previousDateParseHandling, previousFloatParseHandling, previousMaxDepth, previousDateFormatString);
			return value;
		}
		private void SetupReader(JsonReader reader, out CultureInfo previousCulture, out DateTimeZoneHandling? previousDateTimeZoneHandling, out DateParseHandling? previousDateParseHandling, out FloatParseHandling? previousFloatParseHandling, out int? previousMaxDepth, out string previousDateFormatString)
		{
			if (this._culture != null && !this._culture.Equals(reader.Culture))
			{
				previousCulture = reader.Culture;
				reader.Culture = this._culture;
			}
			else
			{
				previousCulture = null;
			}
			if (this._dateTimeZoneHandling.HasValue && reader.DateTimeZoneHandling != this._dateTimeZoneHandling)
			{
				previousDateTimeZoneHandling = new DateTimeZoneHandling?(reader.DateTimeZoneHandling);
				reader.DateTimeZoneHandling = this._dateTimeZoneHandling.Value;
			}
			else
			{
				previousDateTimeZoneHandling = null;
			}
			if (this._dateParseHandling.HasValue && reader.DateParseHandling != this._dateParseHandling)
			{
				previousDateParseHandling = new DateParseHandling?(reader.DateParseHandling);
				reader.DateParseHandling = this._dateParseHandling.Value;
			}
			else
			{
				previousDateParseHandling = null;
			}
			if (this._floatParseHandling.HasValue && reader.FloatParseHandling != this._floatParseHandling)
			{
				previousFloatParseHandling = new FloatParseHandling?(reader.FloatParseHandling);
				reader.FloatParseHandling = this._floatParseHandling.Value;
			}
			else
			{
				previousFloatParseHandling = null;
			}
			if (this._maxDepthSet && reader.MaxDepth != this._maxDepth)
			{
				previousMaxDepth = reader.MaxDepth;
				reader.MaxDepth = this._maxDepth;
			}
			else
			{
				previousMaxDepth = null;
			}
			if (this._dateFormatStringSet && reader.DateFormatString != this._dateFormatString)
			{
				previousDateFormatString = reader.DateFormatString;
				reader.DateFormatString = this._dateFormatString;
			}
			else
			{
				previousDateFormatString = null;
			}
			JsonTextReader textReader = reader as JsonTextReader;
			if (textReader != null)
			{
				DefaultContractResolver resolver = this._contractResolver as DefaultContractResolver;
				if (resolver != null)
				{
					textReader.NameTable = resolver.GetState().NameTable;
				}
			}
		}
		private void ResetReader(JsonReader reader, CultureInfo previousCulture, DateTimeZoneHandling? previousDateTimeZoneHandling, DateParseHandling? previousDateParseHandling, FloatParseHandling? previousFloatParseHandling, int? previousMaxDepth, string previousDateFormatString)
		{
			if (previousCulture != null)
			{
				reader.Culture = previousCulture;
			}
			if (previousDateTimeZoneHandling.HasValue)
			{
				reader.DateTimeZoneHandling = previousDateTimeZoneHandling.Value;
			}
			if (previousDateParseHandling.HasValue)
			{
				reader.DateParseHandling = previousDateParseHandling.Value;
			}
			if (previousFloatParseHandling.HasValue)
			{
				reader.FloatParseHandling = previousFloatParseHandling.Value;
			}
			if (this._maxDepthSet)
			{
				reader.MaxDepth = previousMaxDepth;
			}
			if (this._dateFormatStringSet)
			{
				reader.DateFormatString = previousDateFormatString;
			}
			JsonTextReader textReader = reader as JsonTextReader;
			if (textReader != null)
			{
				textReader.NameTable = null;
			}
		}
		internal void Serialize(TextWriter textWriter, object value)
		{
			this.Serialize(new JsonTextWriter(textWriter), value);
		}
		internal void Serialize(JsonWriter jsonWriter, object value, Type objectType)
		{
			this.SerializeInternal(jsonWriter, value, objectType);
		}
		internal void Serialize(TextWriter textWriter, object value, Type objectType)
		{
			this.Serialize(new JsonTextWriter(textWriter), value, objectType);
		}
		internal void Serialize(JsonWriter jsonWriter, object value)
		{
			this.SerializeInternal(jsonWriter, value, null);
		}
		internal virtual void SerializeInternal(JsonWriter jsonWriter, object value, Type objectType)
		{
			ValidationUtils.ArgumentNotNull(jsonWriter, "jsonWriter");
			Formatting? previousFormatting = null;
			if (this._formatting.HasValue && jsonWriter.Formatting != this._formatting)
			{
				previousFormatting = new Formatting?(jsonWriter.Formatting);
				jsonWriter.Formatting = this._formatting.Value;
			}
			DateFormatHandling? previousDateFormatHandling = null;
			if (this._dateFormatHandling.HasValue && jsonWriter.DateFormatHandling != this._dateFormatHandling)
			{
				previousDateFormatHandling = new DateFormatHandling?(jsonWriter.DateFormatHandling);
				jsonWriter.DateFormatHandling = this._dateFormatHandling.Value;
			}
			DateTimeZoneHandling? previousDateTimeZoneHandling = null;
			if (this._dateTimeZoneHandling.HasValue && jsonWriter.DateTimeZoneHandling != this._dateTimeZoneHandling)
			{
				previousDateTimeZoneHandling = new DateTimeZoneHandling?(jsonWriter.DateTimeZoneHandling);
				jsonWriter.DateTimeZoneHandling = this._dateTimeZoneHandling.Value;
			}
			FloatFormatHandling? previousFloatFormatHandling = null;
			if (this._floatFormatHandling.HasValue && jsonWriter.FloatFormatHandling != this._floatFormatHandling)
			{
				previousFloatFormatHandling = new FloatFormatHandling?(jsonWriter.FloatFormatHandling);
				jsonWriter.FloatFormatHandling = this._floatFormatHandling.Value;
			}
			StringEscapeHandling? previousStringEscapeHandling = null;
			if (this._stringEscapeHandling.HasValue && jsonWriter.StringEscapeHandling != this._stringEscapeHandling)
			{
				previousStringEscapeHandling = new StringEscapeHandling?(jsonWriter.StringEscapeHandling);
				jsonWriter.StringEscapeHandling = this._stringEscapeHandling.Value;
			}
			CultureInfo previousCulture = null;
			if (this._culture != null && !this._culture.Equals(jsonWriter.Culture))
			{
				previousCulture = jsonWriter.Culture;
				jsonWriter.Culture = this._culture;
			}
			string previousDateFormatString = null;
			if (this._dateFormatStringSet && jsonWriter.DateFormatString != this._dateFormatString)
			{
				previousDateFormatString = jsonWriter.DateFormatString;
				jsonWriter.DateFormatString = this._dateFormatString;
			}
			TraceJsonWriter traceJsonWriter = (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose) ? new TraceJsonWriter(jsonWriter) : null;
			JsonSerializerInternalWriter serializerWriter = new JsonSerializerInternalWriter(this);
			serializerWriter.Serialize(traceJsonWriter ?? jsonWriter, value, objectType);
			if (traceJsonWriter != null)
			{
				this.TraceWriter.Trace(TraceLevel.Verbose, "Serialized JSON: " + Environment.NewLine + traceJsonWriter.GetJson(), null);
			}
			if (previousFormatting.HasValue)
			{
				jsonWriter.Formatting = previousFormatting.Value;
			}
			if (previousDateFormatHandling.HasValue)
			{
				jsonWriter.DateFormatHandling = previousDateFormatHandling.Value;
			}
			if (previousDateTimeZoneHandling.HasValue)
			{
				jsonWriter.DateTimeZoneHandling = previousDateTimeZoneHandling.Value;
			}
			if (previousFloatFormatHandling.HasValue)
			{
				jsonWriter.FloatFormatHandling = previousFloatFormatHandling.Value;
			}
			if (previousStringEscapeHandling.HasValue)
			{
				jsonWriter.StringEscapeHandling = previousStringEscapeHandling.Value;
			}
			if (this._dateFormatStringSet)
			{
				jsonWriter.DateFormatString = previousDateFormatString;
			}
			if (previousCulture != null)
			{
				jsonWriter.Culture = previousCulture;
			}
		}
		internal IReferenceResolver GetReferenceResolver()
		{
			if (this._referenceResolver == null)
			{
				this._referenceResolver = new DefaultReferenceResolver();
			}
			return this._referenceResolver;
		}
		internal JsonConverter GetMatchingConverter(Type type)
		{
			return JsonSerializer.GetMatchingConverter(this._converters, type);
		}
		internal static JsonConverter GetMatchingConverter(IList<JsonConverter> converters, Type objectType)
		{
			if (converters != null)
			{
				for (int i = 0; i < converters.Count; i++)
				{
					JsonConverter converter = converters[i];
					if (converter.CanConvert(objectType))
					{
						return converter;
					}
				}
			}
			return null;
		}
		internal void OnError(ErrorEventArgs e)
		{
			EventHandler<ErrorEventArgs> error = this.Error;
			if (error != null)
			{
				error(this, e);
			}
		}
	}
}
