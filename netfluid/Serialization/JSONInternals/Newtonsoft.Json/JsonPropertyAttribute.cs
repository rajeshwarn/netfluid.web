using System;
namespace Newtonsoft.Json
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
	internal sealed class JsonPropertyAttribute : Attribute
	{
		internal NullValueHandling? _nullValueHandling;
		internal DefaultValueHandling? _defaultValueHandling;
		internal ReferenceLoopHandling? _referenceLoopHandling;
		internal ObjectCreationHandling? _objectCreationHandling;
		internal TypeNameHandling? _typeNameHandling;
		internal bool? _isReference;
		internal int? _order;
		internal Required? _required;
		internal bool? _itemIsReference;
		internal ReferenceLoopHandling? _itemReferenceLoopHandling;
		internal TypeNameHandling? _itemTypeNameHandling;
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
		internal int Order
		{
			get
			{
				int? order = this._order;
				if (!order.HasValue)
				{
					return 0;
				}
				return order.GetValueOrDefault();
			}
			set
			{
				this._order = new int?(value);
			}
		}
		internal Required Required
		{
			get
			{
				Required? required = this._required;
				if (!required.HasValue)
				{
					return Required.Default;
				}
				return required.GetValueOrDefault();
			}
			set
			{
				this._required = new Required?(value);
			}
		}
		internal string PropertyName
		{
			get;
			set;
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
		internal JsonPropertyAttribute()
		{
		}
		internal JsonPropertyAttribute(string propertyName)
		{
			this.PropertyName = propertyName;
		}
	}
}
