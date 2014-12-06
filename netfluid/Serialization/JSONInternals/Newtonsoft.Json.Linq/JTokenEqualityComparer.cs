using System;
using System.Collections.Generic;
namespace Newtonsoft.Json.Linq
{
	internal class JTokenEqualityComparer : IEqualityComparer<JToken>
	{
		internal bool Equals(JToken x, JToken y)
		{
			return JToken.DeepEquals(x, y);
		}
		internal int GetHashCode(JToken obj)
		{
			if (obj == null)
			{
				return 0;
			}
			return obj.GetDeepHashCode();
		}
	}
}
