using System;
namespace Newtonsoft.Json
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	internal class JsonExtensionDataAttribute : Attribute
	{
		internal bool WriteData
		{
			get;
			set;
		}
		internal bool ReadData
		{
			get;
			set;
		}
		internal JsonExtensionDataAttribute()
		{
			this.WriteData = true;
			this.ReadData = true;
		}
	}
}
