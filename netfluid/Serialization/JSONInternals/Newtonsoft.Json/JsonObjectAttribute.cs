using System;
namespace Newtonsoft.Json
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
	internal sealed class JsonObjectAttribute : JsonContainerAttribute
	{
		private MemberSerialization _memberSerialization;
		internal Required? _itemRequired;
		internal MemberSerialization MemberSerialization
		{
			get
			{
				return this._memberSerialization;
			}
			set
			{
				this._memberSerialization = value;
			}
		}
		internal Required ItemRequired
		{
			get
			{
				Required? itemRequired = this._itemRequired;
				if (!itemRequired.HasValue)
				{
					return Required.Default;
				}
				return itemRequired.GetValueOrDefault();
			}
			set
			{
				this._itemRequired = new Required?(value);
			}
		}
		internal JsonObjectAttribute()
		{
		}
		internal JsonObjectAttribute(MemberSerialization memberSerialization)
		{
			this.MemberSerialization = memberSerialization;
		}
		internal JsonObjectAttribute(string id) : base(id)
		{
		}
	}
}
