using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Newtonsoft.Json.Linq
{
	internal class JPropertyKeyedCollection : Collection<JToken>
	{
		private static readonly IEqualityComparer<string> Comparer = StringComparer.Ordinal;
		private Dictionary<string, JToken> _dictionary;
		internal JToken this[string key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key");
				}
				if (this._dictionary != null)
				{
					return this._dictionary[key];
				}
				throw new KeyNotFoundException();
			}
		}
		internal ICollection<string> Keys
		{
			get
			{
				this.EnsureDictionary();
				return this._dictionary.Keys;
			}
		}
		internal ICollection<JToken> Values
		{
			get
			{
				this.EnsureDictionary();
				return this._dictionary.Values;
			}
		}
		private void AddKey(string key, JToken item)
		{
			this.EnsureDictionary();
			this._dictionary[key] = item;
		}
		protected void ChangeItemKey(JToken item, string newKey)
		{
			if (!this.ContainsItem(item))
			{
				throw new ArgumentException("The specified item does not exist in this KeyedCollection.");
			}
			string keyForItem = this.GetKeyForItem(item);
			if (!JPropertyKeyedCollection.Comparer.Equals(keyForItem, newKey))
			{
				if (newKey != null)
				{
					this.AddKey(newKey, item);
				}
				if (keyForItem != null)
				{
					this.RemoveKey(keyForItem);
				}
			}
		}
		protected override void ClearItems()
		{
			base.ClearItems();
			if (this._dictionary != null)
			{
				this._dictionary.Clear();
			}
		}
		internal bool Contains(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			return this._dictionary != null && this._dictionary.ContainsKey(key);
		}
		private bool ContainsItem(JToken item)
		{
			if (this._dictionary == null)
			{
				return false;
			}
			string key = this.GetKeyForItem(item);
			JToken value;
			return this._dictionary.TryGetValue(key, out value);
		}
		private void EnsureDictionary()
		{
			if (this._dictionary == null)
			{
				this._dictionary = new Dictionary<string, JToken>(JPropertyKeyedCollection.Comparer);
			}
		}
		private string GetKeyForItem(JToken item)
		{
			return ((JProperty)item).Name;
		}
		protected override void InsertItem(int index, JToken item)
		{
			this.AddKey(this.GetKeyForItem(item), item);
			base.InsertItem(index, item);
		}
		internal bool Remove(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			return this._dictionary != null && this._dictionary.ContainsKey(key) && base.Remove(this._dictionary[key]);
		}
		protected override void RemoveItem(int index)
		{
			string keyForItem = this.GetKeyForItem(base.Items[index]);
			this.RemoveKey(keyForItem);
			base.RemoveItem(index);
		}
		private void RemoveKey(string key)
		{
			if (this._dictionary != null)
			{
				this._dictionary.Remove(key);
			}
		}
		protected override void SetItem(int index, JToken item)
		{
			string keyForItem = this.GetKeyForItem(item);
			string keyAtIndex = this.GetKeyForItem(base.Items[index]);
			if (JPropertyKeyedCollection.Comparer.Equals(keyAtIndex, keyForItem))
			{
				if (this._dictionary != null)
				{
					this._dictionary[keyForItem] = item;
				}
			}
			else
			{
				this.AddKey(keyForItem, item);
				if (keyAtIndex != null)
				{
					this.RemoveKey(keyAtIndex);
				}
			}
			base.SetItem(index, item);
		}
		internal bool TryGetValue(string key, out JToken value)
		{
			if (this._dictionary == null)
			{
				value = null;
				return false;
			}
			return this._dictionary.TryGetValue(key, out value);
		}
		internal bool Compare(JPropertyKeyedCollection other)
		{
			if (this == other)
			{
				return true;
			}
			Dictionary<string, JToken> d = this._dictionary;
			Dictionary<string, JToken> d2 = other._dictionary;
			if (d == null && d2 == null)
			{
				return true;
			}
			if (d == null)
			{
				return d2.Count == 0;
			}
			if (d2 == null)
			{
				return d.Count == 0;
			}
			if (d.Count != d2.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, JToken> keyAndProperty in d)
			{
				JToken secondValue;
				if (!d2.TryGetValue(keyAndProperty.Key, out secondValue))
				{
					bool result = false;
					return result;
				}
				JProperty p = (JProperty)keyAndProperty.Value;
				JProperty p2 = (JProperty)secondValue;
				if (p.Value == null)
				{
					bool result = p2.Value == null;
					return result;
				}
				if (!p.Value.DeepEquals(p2.Value))
				{
					bool result = false;
					return result;
				}
			}
			return true;
		}
	}
}
