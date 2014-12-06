using System;
namespace Newtonsoft.Json.Utilities
{
	internal class PropertyNameTable
	{
		private class Entry
		{
			internal readonly string Value;
			internal readonly int HashCode;
			internal PropertyNameTable.Entry Next;
			internal Entry(string value, int hashCode, PropertyNameTable.Entry next)
			{
				this.Value = value;
				this.HashCode = hashCode;
				this.Next = next;
			}
		}
		private static readonly int HashCodeRandomizer;
		private int _count;
		private PropertyNameTable.Entry[] _entries;
		private int _mask = 31;
		static PropertyNameTable()
		{
			PropertyNameTable.HashCodeRandomizer = Environment.TickCount;
		}
		internal PropertyNameTable()
		{
			this._entries = new PropertyNameTable.Entry[this._mask + 1];
		}
		internal string Get(char[] key, int start, int length)
		{
			if (length == 0)
			{
				return string.Empty;
			}
			int hashCode = length + PropertyNameTable.HashCodeRandomizer;
			hashCode += (hashCode << 7 ^ (int)key[start]);
			int end = start + length;
			for (int i = start + 1; i < end; i++)
			{
				hashCode += (hashCode << 7 ^ (int)key[i]);
			}
			hashCode -= hashCode >> 17;
			hashCode -= hashCode >> 11;
			hashCode -= hashCode >> 5;
			for (PropertyNameTable.Entry entry = this._entries[hashCode & this._mask]; entry != null; entry = entry.Next)
			{
				if (entry.HashCode == hashCode && PropertyNameTable.TextEquals(entry.Value, key, start, length))
				{
					return entry.Value;
				}
			}
			return null;
		}
		internal string Add(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int length = key.Length;
			if (length == 0)
			{
				return string.Empty;
			}
			int hashCode = length + PropertyNameTable.HashCodeRandomizer;
			for (int i = 0; i < key.Length; i++)
			{
				hashCode += (hashCode << 7 ^ (int)key[i]);
			}
			hashCode -= hashCode >> 17;
			hashCode -= hashCode >> 11;
			hashCode -= hashCode >> 5;
			for (PropertyNameTable.Entry entry = this._entries[hashCode & this._mask]; entry != null; entry = entry.Next)
			{
				if (entry.HashCode == hashCode && entry.Value.Equals(key))
				{
					return entry.Value;
				}
			}
			return this.AddEntry(key, hashCode);
		}
		private string AddEntry(string str, int hashCode)
		{
			int index = hashCode & this._mask;
			PropertyNameTable.Entry entry = new PropertyNameTable.Entry(str, hashCode, this._entries[index]);
			this._entries[index] = entry;
			if (this._count++ == this._mask)
			{
				this.Grow();
			}
			return entry.Value;
		}
		private void Grow()
		{
			PropertyNameTable.Entry[] entries = this._entries;
			int newMask = this._mask * 2 + 1;
			PropertyNameTable.Entry[] newEntries = new PropertyNameTable.Entry[newMask + 1];
			for (int i = 0; i < entries.Length; i++)
			{
				PropertyNameTable.Entry next;
				for (PropertyNameTable.Entry entry = entries[i]; entry != null; entry = next)
				{
					int index = entry.HashCode & newMask;
					next = entry.Next;
					entry.Next = newEntries[index];
					newEntries[index] = entry;
				}
			}
			this._entries = newEntries;
			this._mask = newMask;
		}
		private static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length)
		{
			if (str1.Length != str2Length)
			{
				return false;
			}
			for (int i = 0; i < str1.Length; i++)
			{
				if (str1[i] != str2[str2Start + i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
