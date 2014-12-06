using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
namespace Newtonsoft.Json
{
	internal class JsonSerializerSettings
	{
		internal const ReferenceLoopHandling DefaultReferenceLoopHandling = ReferenceLoopHandling.Error;
		internal const MissingMemberHandling DefaultMissingMemberHandling = MissingMemberHandling.Ignore;
		internal const NullValueHandling DefaultNullValueHandling = NullValueHandling.Include;
		internal const DefaultValueHandling DefaultDefaultValueHandling = DefaultValueHandling.Include;
		internal const ObjectCreationHandling DefaultObjectCreationHandling = ObjectCreationHandling.Auto;
		internal const PreserveReferencesHandling DefaultPreserveReferencesHandling = PreserveReferencesHandling.None;
		internal const ConstructorHandling DefaultConstructorHandling = ConstructorHandling.Default;
		internal const TypeNameHandling DefaultTypeNameHandling = TypeNameHandling.None;
		internal const MetadataPropertyHandling DefaultMetadataPropertyHandling = MetadataPropertyHandling.Default;
		internal const FormatterAssemblyStyle DefaultTypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
		internal const Formatting DefaultFormatting = Formatting.None;
		internal const DateFormatHandling DefaultDateFormatHandling = DateFormatHandling.IsoDateFormat;
		internal const DateTimeZoneHandling DefaultDateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
		internal const DateParseHandling DefaultDateParseHandling = DateParseHandling.DateTime;
		internal const FloatParseHandling DefaultFloatParseHandling = FloatParseHandling.Double;
		internal const FloatFormatHandling DefaultFloatFormatHandling = FloatFormatHandling.String;
		internal const StringEscapeHandling DefaultStringEscapeHandling = StringEscapeHandling.Default;
		internal const FormatterAssemblyStyle DefaultFormatterAssemblyStyle = FormatterAssemblyStyle.Simple;
		internal const bool DefaultCheckAdditionalContent = false;
		internal const string DefaultDateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
		internal static readonly StreamingContext DefaultContext;
		internal static readonly CultureInfo DefaultCulture;
		internal Formatting? _formatting;
		internal DateFormatHandling? _dateFormatHandling;
		internal DateTimeZoneHandling? _dateTimeZoneHandling;
		internal DateParseHandling? _dateParseHandling;
		internal FloatFormatHandling? _floatFormatHandling;
		internal FloatParseHandling? _floatParseHandling;
		internal StringEscapeHandling? _stringEscapeHandling;
		internal CultureInfo _culture;
		internal bool? _checkAdditionalContent;
		internal int? _maxDepth;
		internal bool _maxDepthSet;
		internal string _dateFormatString;
		internal bool _dateFormatStringSet;
		internal FormatterAssemblyStyle? _typeNameAssemblyFormat;
		internal DefaultValueHandling? _defaultValueHandling;
		internal PreserveReferencesHandling? _preserveReferencesHandling;
		internal NullValueHandling? _nullValueHandling;
		internal ObjectCreationHandling? _objectCreationHandling;
		internal MissingMemberHandling? _missingMemberHandling;
		internal ReferenceLoopHandling? _referenceLoopHandling;
		internal StreamingContext? _context;
		internal ConstructorHandling? _constructorHandling;
		internal TypeNameHandling? _typeNameHandling;
		internal MetadataPropertyHandling? _metadataPropertyHandling;
		internal ReferenceLoopHandling ReferenceLoopHandling
		{
			get
			{
				ReferenceLoopHandling? referenceLoopHandling = this._referenceLoopHandling;
				if (!referenceLoopHandling.HasValue)
				{
					return ReferenceLoopHandling.Error;
				}
				return referenceLoopHandling.GetValueOrDefault();
			}
			set
			{
				this._referenceLoopHandling = new ReferenceLoopHandling?(value);
			}
		}
		internal MissingMemberHandling MissingMemberHandling
		{
			get
			{
				MissingMemberHandling? missingMemberHandling = this._missingMemberHandling;
				if (!missingMemberHandling.HasValue)
				{
					return MissingMemberHandling.Ignore;
				}
				return missingMemberHandling.GetValueOrDefault();
			}
			set
			{
				this._missingMemberHandling = new MissingMemberHandling?(value);
			}
		}
		internal ObjectCreationHandling ObjectCreationHandling
		{
			get
			{
				ObjectCreationHandling? objectCreationHandling = this._objectCreationHandling;
				if (!objectCreationHandling.HasValue)
				{
					return ObjectCreationHandling.Auto;
				}
				return objectCreationHandling.GetValueOrDefault();
			}
			set
			{
				this._objectCreationHandling = new ObjectCreationHandling?(value);
			}
		}
		internal NullValueHandling NullValueHandling
		{
			get
			{
				NullValueHandling? nullValueHandling = this._nullValueHandling;
				if (!nullValueHandling.HasValue)
				{
					return NullValueHandling.Include;
				}
				return nullValueHandling.GetValueOrDefault();
			}
			set
			{
				this._nullValueHandling = new NullValueHandling?(value);
			}
		}
		internal DefaultValueHandling DefaultValueHandling
		{
			get
			{
				DefaultValueHandling? defaultValueHandling = this._defaultValueHandling;
				if (!defaultValueHandling.HasValue)
				{
					return DefaultValueHandling.Include;
				}
				return defaultValueHandling.GetValueOrDefault();
			}
			set
			{
				this._defaultValueHandling = new DefaultValueHandling?(value);
			}
		}
		internal IList<JsonConverter> Converters
		{
			get;
			set;
		}
		internal PreserveReferencesHandling PreserveReferencesHandling
		{
			get
			{
				PreserveReferencesHandling? preserveReferencesHandling = this._preserveReferencesHandling;
				if (!preserveReferencesHandling.HasValue)
				{
					return PreserveReferencesHandling.None;
				}
				return preserveReferencesHandling.GetValueOrDefault();
			}
			set
			{
				this._preserveReferencesHandling = new PreserveReferencesHandling?(value);
			}
		}
		internal TypeNameHandling TypeNameHandling
		{
			get
			{
				TypeNameHandling? typeNameHandling = this._typeNameHandling;
				if (!typeNameHandling.HasValue)
				{
					return TypeNameHandling.None;
				}
				return typeNameHandling.GetValueOrDefault();
			}
			set
			{
				this._typeNameHandling = new TypeNameHandling?(value);
			}
		}
		internal MetadataPropertyHandling MetadataPropertyHandling
		{
			get
			{
				MetadataPropertyHandling? metadataPropertyHandling = this._metadataPropertyHandling;
				if (!metadataPropertyHandling.HasValue)
				{
					return MetadataPropertyHandling.Default;
				}
				return metadataPropertyHandling.GetValueOrDefault();
			}
			set
			{
				this._metadataPropertyHandling = new MetadataPropertyHandling?(value);
			}
		}
		internal FormatterAssemblyStyle TypeNameAssemblyFormat
		{
			get
			{
				FormatterAssemblyStyle? typeNameAssemblyFormat = this._typeNameAssemblyFormat;
				if (!typeNameAssemblyFormat.HasValue)
				{
					return FormatterAssemblyStyle.Simple;
				}
				return typeNameAssemblyFormat.GetValueOrDefault();
			}
			set
			{
				this._typeNameAssemblyFormat = new FormatterAssemblyStyle?(value);
			}
		}
		internal ConstructorHandling ConstructorHandling
		{
			get
			{
				ConstructorHandling? constructorHandling = this._constructorHandling;
				if (!constructorHandling.HasValue)
				{
					return ConstructorHandling.Default;
				}
				return constructorHandling.GetValueOrDefault();
			}
			set
			{
				this._constructorHandling = new ConstructorHandling?(value);
			}
		}
		internal IContractResolver ContractResolver
		{
			get;
			set;
		}
		internal IReferenceResolver ReferenceResolver
		{
			get;
			set;
		}
		internal ITraceWriter TraceWriter
		{
			get;
			set;
		}
		internal SerializationBinder Binder
		{
			get;
			set;
		}
		internal EventHandler<ErrorEventArgs> Error
		{
			get;
			set;
		}
		internal StreamingContext Context
		{
			get
			{
				StreamingContext? context = this._context;
				if (!context.HasValue)
				{
					return JsonSerializerSettings.DefaultContext;
				}
				return context.GetValueOrDefault();
			}
			set
			{
				this._context = new StreamingContext?(value);
			}
		}
		internal string DateFormatString
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
				this._maxDepthSet = true;
			}
		}
		internal Formatting Formatting
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
		internal DateFormatHandling DateFormatHandling
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
		internal DateTimeZoneHandling DateTimeZoneHandling
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
		internal DateParseHandling DateParseHandling
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
		internal FloatFormatHandling FloatFormatHandling
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
		internal FloatParseHandling FloatParseHandling
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
		internal StringEscapeHandling StringEscapeHandling
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
		internal CultureInfo Culture
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
		internal bool CheckAdditionalContent
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
		static JsonSerializerSettings()
		{
			JsonSerializerSettings.DefaultContext = default(StreamingContext);
			JsonSerializerSettings.DefaultCulture = CultureInfo.InvariantCulture;
		}
		internal JsonSerializerSettings()
		{
			this.Converters = new List<JsonConverter>();
		}
	}
}
