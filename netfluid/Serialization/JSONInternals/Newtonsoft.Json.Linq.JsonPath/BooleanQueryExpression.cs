using System;
using System.Collections.Generic;
namespace Newtonsoft.Json.Linq.JsonPath
{
	internal class BooleanQueryExpression : QueryExpression
	{
		internal List<PathFilter> Path
		{
			get;
			set;
		}
		internal JValue Value
		{
			get;
			set;
		}
		internal override bool IsMatch(JToken t)
		{
			IEnumerable<JToken> pathResult = JPath.Evaluate(this.Path, t, false);
			foreach (JToken r in pathResult)
			{
				JValue v = r as JValue;
				switch (base.Operator)
				{
				case QueryOperator.Equals:
					if (v != null && v.Equals(this.Value))
					{
						bool result = true;
						return result;
					}
					break;
				case QueryOperator.NotEquals:
					if (v != null && !v.Equals(this.Value))
					{
						bool result = true;
						return result;
					}
					break;
				case QueryOperator.Exists:
				{
					bool result = true;
					return result;
				}
				case QueryOperator.LessThan:
					if (v != null && v.CompareTo(this.Value) < 0)
					{
						bool result = true;
						return result;
					}
					break;
				case QueryOperator.LessThanOrEquals:
					if (v != null && v.CompareTo(this.Value) <= 0)
					{
						bool result = true;
						return result;
					}
					break;
				case QueryOperator.GreaterThan:
					if (v != null && v.CompareTo(this.Value) > 0)
					{
						bool result = true;
						return result;
					}
					break;
				case QueryOperator.GreaterThanOrEquals:
					if (v != null && v.CompareTo(this.Value) >= 0)
					{
						bool result = true;
						return result;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
			return false;
		}
	}
}
