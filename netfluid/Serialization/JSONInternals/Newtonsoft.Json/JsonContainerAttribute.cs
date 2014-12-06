using System;
namespace Newtonsoft.Json
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
	internal abstract class JsonContainerAttribute : Attribute
	{
		internal bool? _isReference;
		internal bool? _itemIsReference;
		internal ReferenceLoopHandling? _itemReferenceLoopHandling;
		internal TypeNameHandling? _itemTypeNameHandling;
		internal string Id
		{
			get;
			set;
		}
		internal string Title
		{
			get;
			set;
		}
		internal string Description
		{
			get;
			set;
		}
		internal Type ItemConverterType
		{
			get;
			set;
		}
		internal object[] ItemConverterParameters
		{
			get;
			set;
		}
		internal bool IsReference
		{
			get
			{
				return this._isReference ?? false;
			}
			set
			{
				this._isReference = new bool?(value);
			}
		}
		internal bool ItemIsReference
		{
			get
			{
				return this._itemIsReference ?? false;
			}
			set
			{
				this._itemIsReference = new bool?(value);
			}
		}
		internal ReferenceLoopHandling ItemReferenceLoopHandling
		{
			get
			{
				ReferenceLoopHandling? itemReferenceLoopHandling = this._itemReferenceLoopHandling;
				if (!itemReferenceLoopHandling.HasValue)
				{
					return ReferenceLoopHandling.Error;
				}
				return itemReferenceLoopHandling.GetValueOrDefault();
			}
			set
			{
				this._itemReferenceLoopHandling = new ReferenceLoopHandling?(value);
			}
		}
		internal TypeNameHandling ItemTypeNameHandling
		{
			get
			{
				TypeNameHandling? itemTypeNameHandling = this._itemTypeNameHandling;
				if (!itemTypeNameHandling.HasValue)
				{
					return TypeNameHandling.None;
				}
				return itemTypeNameHandling.GetValueOrDefault();
			}
			set
			{
				this._itemTypeNameHandling = new TypeNameHandling?(value);
			}
		}
		protected JsonContainerAttribute()
		{
		}
		protected JsonContainerAttribute(string id)
		{
			this.Id = id;
		}
	}
}
