using System;
namespace Newtonsoft.Json
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
	internal sealed class JsonArrayAttribute : JsonContainerAttribute
	{
		private bool _allowNullItems;
		internal bool AllowNullItems
		{
			get
			{
				return this._allowNullItems;
			}
			set
			{
				this._allowNullItems = value;
			}
		}
		internal JsonArrayAttribute()
		{
		}
		internal JsonArrayAttribute(bool allowNullItems)
		{
			this._allowNullItems = allowNullItems;
		}
		internal JsonArrayAttribute(string id) : base(id)
		{
		}
	}
}
