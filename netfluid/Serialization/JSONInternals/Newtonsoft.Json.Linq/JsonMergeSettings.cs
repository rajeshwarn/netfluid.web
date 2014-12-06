using System;
namespace Newtonsoft.Json.Linq
{
	internal class JsonMergeSettings
	{
		private MergeArrayHandling _mergeArrayHandling;
		internal MergeArrayHandling MergeArrayHandling
		{
			get
			{
				return this._mergeArrayHandling;
			}
			set
			{
				if (value < MergeArrayHandling.Concat || value > MergeArrayHandling.Merge)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._mergeArrayHandling = value;
			}
		}
	}
}
