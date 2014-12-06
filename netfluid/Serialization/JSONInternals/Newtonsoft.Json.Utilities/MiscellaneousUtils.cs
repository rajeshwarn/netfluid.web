using System;
using System.Globalization;
namespace Newtonsoft.Json.Utilities
{
	internal static class MiscellaneousUtils
	{
		internal static bool ValueEquals(object objA, object objB)
		{
			if (objA == null && objB == null)
			{
				return true;
			}
			if (objA != null && objB == null)
			{
				return false;
			}
			if (objA == null && objB != null)
			{
				return false;
			}
			if (objA.GetType() == objB.GetType())
			{
				return objA.Equals(objB);
			}
			if (ConvertUtils.IsInteger(objA) && ConvertUtils.IsInteger(objB))
			{
				return Convert.ToDecimal(objA, CultureInfo.CurrentCulture).Equals(Convert.ToDecimal(objB, CultureInfo.CurrentCulture));
			}
			return (objA is double || objA is float || objA is decimal) && (objB is double || objB is float || objB is decimal) && MathUtils.ApproxEquals(Convert.ToDouble(objA, CultureInfo.CurrentCulture), Convert.ToDouble(objB, CultureInfo.CurrentCulture));
		}
		internal static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string paramName, object actualValue, string message)
		{
			string newMessage = message + Environment.NewLine + "Actual value was {0}.".FormatWith(CultureInfo.InvariantCulture, actualValue);
			return new ArgumentOutOfRangeException(paramName, newMessage);
		}
		internal static string ToString(object value)
		{
			if (value == null)
			{
				return "{null}";
			}
			if (!(value is string))
			{
				return value.ToString();
			}
			return "\"" + value.ToString() + "\"";
		}
		internal static int ByteArrayCompare(byte[] a1, byte[] a2)
		{
			int lengthCompare = a1.Length.CompareTo(a2.Length);
			if (lengthCompare != 0)
			{
				return lengthCompare;
			}
			for (int i = 0; i < a1.Length; i++)
			{
				int valueCompare = a1[i].CompareTo(a2[i]);
				if (valueCompare != 0)
				{
					return valueCompare;
				}
			}
			return 0;
		}
		internal static string GetPrefix(string qualifiedName)
		{
			string prefix;
			string localName;
			MiscellaneousUtils.GetQualifiedNameParts(qualifiedName, out prefix, out localName);
			return prefix;
		}
		internal static string GetLocalName(string qualifiedName)
		{
			string prefix;
			string localName;
			MiscellaneousUtils.GetQualifiedNameParts(qualifiedName, out prefix, out localName);
			return localName;
		}
		internal static void GetQualifiedNameParts(string qualifiedName, out string prefix, out string localName)
		{
			int colonPosition = qualifiedName.IndexOf(':');
			if (colonPosition == -1 || colonPosition == 0 || qualifiedName.Length - 1 == colonPosition)
			{
				prefix = null;
				localName = qualifiedName;
				return;
			}
			prefix = qualifiedName.Substring(0, colonPosition);
			localName = qualifiedName.Substring(colonPosition + 1);
		}
		internal static string FormatValueForPrint(object value)
		{
			if (value == null)
			{
				return "{null}";
			}
			if (value is string)
			{
				return "\"" + value + "\"";
			}
			return value.ToString();
		}
	}
}
