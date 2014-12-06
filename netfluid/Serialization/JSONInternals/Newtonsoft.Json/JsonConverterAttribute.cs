using System;
namespace Newtonsoft.Json
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter, AllowMultiple = false)]
	internal sealed class JsonConverterAttribute : Attribute
	{
		private readonly Type _converterType;
		internal Type ConverterType
		{
			get
			{
				return this._converterType;
			}
		}
		internal object[] ConverterParameters
		{
			get;
			private set;
		}
		internal JsonConverterAttribute(Type converterType)
		{
			if (converterType == null)
			{
				throw new ArgumentNullException("converterType");
			}
			this._converterType = converterType;
		}
		internal JsonConverterAttribute(Type converterType, params object[] converterParameters) : this(converterType)
		{
			this.ConverterParameters = converterParameters;
		}
	}
}
