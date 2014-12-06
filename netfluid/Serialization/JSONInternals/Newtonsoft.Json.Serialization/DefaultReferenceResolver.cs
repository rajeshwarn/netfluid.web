using Newtonsoft.Json.Utilities;
using System;
using System.Globalization;
namespace Newtonsoft.Json.Serialization
{
	internal class DefaultReferenceResolver : IReferenceResolver
	{
		private int _referenceCount;
		private BidirectionalDictionary<string, object> GetMappings(object context)
		{
			JsonSerializerInternalBase internalSerializer;
			if (context is JsonSerializerInternalBase)
			{
				internalSerializer = (JsonSerializerInternalBase)context;
			}
			else
			{
				if (!(context is JsonSerializerProxy))
				{
					throw new JsonException("The DefaultReferenceResolver can only be used internally.");
				}
				internalSerializer = ((JsonSerializerProxy)context).GetInternalSerializer();
			}
			return internalSerializer.DefaultReferenceMappings;
		}
		internal object ResolveReference(object context, string reference)
		{
			object value;
			this.GetMappings(context).TryGetByFirst(reference, out value);
			return value;
		}
		internal string GetReference(object context, object value)
		{
			BidirectionalDictionary<string, object> mappings = this.GetMappings(context);
			string reference;
			if (!mappings.TryGetBySecond(value, out reference))
			{
				this._referenceCount++;
				reference = this._referenceCount.ToString(CultureInfo.InvariantCulture);
				mappings.Set(reference, value);
			}
			return reference;
		}
		internal void AddReference(object context, string reference, object value)
		{
			this.GetMappings(context).Set(reference, value);
		}
		internal bool IsReferenced(object context, object value)
		{
			string reference;
			return this.GetMappings(context).TryGetBySecond(value, out reference);
		}
	}
}
