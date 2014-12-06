using System;
using System.Globalization;
using System.IO;
namespace Newtonsoft.Json.Utilities
{
	internal static class DateTimeUtils
	{
		private const int DaysPer100Years = 36524;
		private const int DaysPer400Years = 146097;
		private const int DaysPer4Years = 1461;
		private const int DaysPerYear = 365;
		private const long TicksPerDay = 864000000000L;
		internal static readonly long InitialJavaScriptDateTicks;
		private static readonly int[] DaysToMonth365;
		private static readonly int[] DaysToMonth366;
		static DateTimeUtils()
		{
			DateTimeUtils.InitialJavaScriptDateTicks = 621355968000000000L;
			DateTimeUtils.DaysToMonth365 = new int[]
			{
				0,
				31,
				59,
				90,
				120,
				151,
				181,
				212,
				243,
				273,
				304,
				334,
				365
			};
			DateTimeUtils.DaysToMonth366 = new int[]
			{
				0,
				31,
				60,
				91,
				121,
				152,
				182,
				213,
				244,
				274,
				305,
				335,
				366
			};
		}
		internal static TimeSpan GetUtcOffset(this DateTime d)
		{
			return TimeZoneInfo.Local.GetUtcOffset(d);
		}
		internal static DateTime EnsureDateTime(DateTime value, DateTimeZoneHandling timeZone)
		{
			switch (timeZone)
			{
			case DateTimeZoneHandling.Local:
				value = DateTimeUtils.SwitchToLocalTime(value);
				break;
			case DateTimeZoneHandling.Utc:
				value = DateTimeUtils.SwitchToUtcTime(value);
				break;
			case DateTimeZoneHandling.Unspecified:
				value = new DateTime(value.Ticks, DateTimeKind.Unspecified);
				break;
			case DateTimeZoneHandling.RoundtripKind:
				break;
			default:
				throw new ArgumentException("Invalid date time handling value.");
			}
			return value;
		}
		private static DateTime SwitchToLocalTime(DateTime value)
		{
			switch (value.Kind)
			{
			case DateTimeKind.Unspecified:
				return new DateTime(value.Ticks, DateTimeKind.Local);
			case DateTimeKind.Utc:
				return value.ToLocalTime();
			case DateTimeKind.Local:
				return value;
			default:
				return value;
			}
		}
		private static DateTime SwitchToUtcTime(DateTime value)
		{
			switch (value.Kind)
			{
			case DateTimeKind.Unspecified:
				return new DateTime(value.Ticks, DateTimeKind.Utc);
			case DateTimeKind.Utc:
				return value;
			case DateTimeKind.Local:
				return value.ToUniversalTime();
			default:
				return value;
			}
		}
		private static long ToUniversalTicks(DateTime dateTime)
		{
			if (dateTime.Kind == DateTimeKind.Utc)
			{
				return dateTime.Ticks;
			}
			return DateTimeUtils.ToUniversalTicks(dateTime, dateTime.GetUtcOffset());
		}
		private static long ToUniversalTicks(DateTime dateTime, TimeSpan offset)
		{
			if (dateTime.Kind == DateTimeKind.Utc || dateTime == DateTime.MaxValue || dateTime == DateTime.MinValue)
			{
				return dateTime.Ticks;
			}
			long ticks = dateTime.Ticks - offset.Ticks;
			if (ticks > 3155378975999999999L)
			{
				return 3155378975999999999L;
			}
			if (ticks < 0L)
			{
				return 0L;
			}
			return ticks;
		}
		internal static long ConvertDateTimeToJavaScriptTicks(DateTime dateTime, TimeSpan offset)
		{
			long universialTicks = DateTimeUtils.ToUniversalTicks(dateTime, offset);
			return DateTimeUtils.UniversialTicksToJavaScriptTicks(universialTicks);
		}
		internal static long ConvertDateTimeToJavaScriptTicks(DateTime dateTime)
		{
			return DateTimeUtils.ConvertDateTimeToJavaScriptTicks(dateTime, true);
		}
		internal static long ConvertDateTimeToJavaScriptTicks(DateTime dateTime, bool convertToUtc)
		{
			long ticks = convertToUtc ? DateTimeUtils.ToUniversalTicks(dateTime) : dateTime.Ticks;
			return DateTimeUtils.UniversialTicksToJavaScriptTicks(ticks);
		}
		private static long UniversialTicksToJavaScriptTicks(long universialTicks)
		{
			return (universialTicks - DateTimeUtils.InitialJavaScriptDateTicks) / 10000L;
		}
		internal static DateTime ConvertJavaScriptTicksToDateTime(long javaScriptTicks)
		{
			DateTime dateTime = new DateTime(javaScriptTicks * 10000L + DateTimeUtils.InitialJavaScriptDateTicks, DateTimeKind.Utc);
			return dateTime;
		}
		internal static bool TryParseDateIso(string text, DateParseHandling dateParseHandling, DateTimeZoneHandling dateTimeZoneHandling, out object dt)
		{
			DateTimeParser dateTimeParser = default(DateTimeParser);
			if (!dateTimeParser.Parse(text))
			{
				dt = null;
				return false;
			}
			DateTime d = new DateTime(dateTimeParser.Year, dateTimeParser.Month, dateTimeParser.Day, dateTimeParser.Hour, dateTimeParser.Minute, dateTimeParser.Second);
			d = d.AddTicks((long)dateTimeParser.Fraction);
			if (dateParseHandling != DateParseHandling.DateTimeOffset)
			{
				switch (dateTimeParser.Zone)
				{
				case ParserTimeZone.Utc:
					d = new DateTime(d.Ticks, DateTimeKind.Utc);
					break;
				case ParserTimeZone.LocalWestOfUtc:
				{
					TimeSpan offset = new TimeSpan(dateTimeParser.ZoneHour, dateTimeParser.ZoneMinute, 0);
					long ticks = d.Ticks + offset.Ticks;
					if (ticks <= DateTime.MaxValue.Ticks)
					{
						d = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
					}
					else
					{
						ticks += d.GetUtcOffset().Ticks;
						if (ticks > DateTime.MaxValue.Ticks)
						{
							ticks = DateTime.MaxValue.Ticks;
						}
						d = new DateTime(ticks, DateTimeKind.Local);
					}
					break;
				}
				case ParserTimeZone.LocalEastOfUtc:
				{
					TimeSpan offset2 = new TimeSpan(dateTimeParser.ZoneHour, dateTimeParser.ZoneMinute, 0);
					long ticks = d.Ticks - offset2.Ticks;
					if (ticks >= DateTime.MinValue.Ticks)
					{
						d = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
					}
					else
					{
						ticks += d.GetUtcOffset().Ticks;
						if (ticks < DateTime.MinValue.Ticks)
						{
							ticks = DateTime.MinValue.Ticks;
						}
						d = new DateTime(ticks, DateTimeKind.Local);
					}
					break;
				}
				}
				dt = DateTimeUtils.EnsureDateTime(d, dateTimeZoneHandling);
				return true;
			}
			TimeSpan offset3;
			switch (dateTimeParser.Zone)
			{
			case ParserTimeZone.Utc:
				offset3 = new TimeSpan(0L);
				break;
			case ParserTimeZone.LocalWestOfUtc:
				offset3 = new TimeSpan(-dateTimeParser.ZoneHour, -dateTimeParser.ZoneMinute, 0);
				break;
			case ParserTimeZone.LocalEastOfUtc:
				offset3 = new TimeSpan(dateTimeParser.ZoneHour, dateTimeParser.ZoneMinute, 0);
				break;
			default:
				offset3 = TimeZoneInfo.Local.GetUtcOffset(d);
				break;
			}
			long ticks2 = d.Ticks - offset3.Ticks;
			if (ticks2 < 0L || ticks2 > 3155378975999999999L)
			{
				dt = null;
				return false;
			}
			dt = new DateTimeOffset(d, offset3);
			return true;
		}
		internal static bool TryParseDateTime(string s, DateParseHandling dateParseHandling, DateTimeZoneHandling dateTimeZoneHandling, string dateFormatString, CultureInfo culture, out object dt)
		{
			if (s.Length > 0)
			{
				if (s[0] == '/')
				{
					if (s.StartsWith("/Date(", StringComparison.Ordinal) && s.EndsWith(")/", StringComparison.Ordinal) && DateTimeUtils.TryParseDateMicrosoft(s, dateParseHandling, dateTimeZoneHandling, out dt))
					{
						return true;
					}
				}
				else
				{
					if (s.Length >= 19 && s.Length <= 40 && char.IsDigit(s[0]) && s[10] == 'T' && DateTimeUtils.TryParseDateIso(s, dateParseHandling, dateTimeZoneHandling, out dt))
					{
						return true;
					}
				}
				if (!string.IsNullOrEmpty(dateFormatString) && DateTimeUtils.TryParseDateExact(s, dateParseHandling, dateTimeZoneHandling, dateFormatString, culture, out dt))
				{
					return true;
				}
			}
			dt = null;
			return false;
		}
		private static bool TryParseDateMicrosoft(string text, DateParseHandling dateParseHandling, DateTimeZoneHandling dateTimeZoneHandling, out object dt)
		{
			string value = text.Substring(6, text.Length - 8);
			DateTimeKind kind = DateTimeKind.Utc;
			int index = value.IndexOf('+', 1);
			if (index == -1)
			{
				index = value.IndexOf('-', 1);
			}
			TimeSpan offset = TimeSpan.Zero;
			if (index != -1)
			{
				kind = DateTimeKind.Local;
				offset = DateTimeUtils.ReadOffset(value.Substring(index));
				value = value.Substring(0, index);
			}
			long javaScriptTicks;
			if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out javaScriptTicks))
			{
				dt = null;
				return false;
			}
			DateTime utcDateTime = DateTimeUtils.ConvertJavaScriptTicksToDateTime(javaScriptTicks);
			if (dateParseHandling == DateParseHandling.DateTimeOffset)
			{
				dt = new DateTimeOffset(utcDateTime.Add(offset).Ticks, offset);
				return true;
			}
			DateTime dateTime;
			switch (kind)
			{
			case DateTimeKind.Unspecified:
				dateTime = DateTime.SpecifyKind(utcDateTime.ToLocalTime(), DateTimeKind.Unspecified);
				goto IL_C6;
			case DateTimeKind.Local:
				dateTime = utcDateTime.ToLocalTime();
				goto IL_C6;
			}
			dateTime = utcDateTime;
			IL_C6:
			dt = DateTimeUtils.EnsureDateTime(dateTime, dateTimeZoneHandling);
			return true;
		}
		private static bool TryParseDateExact(string text, DateParseHandling dateParseHandling, DateTimeZoneHandling dateTimeZoneHandling, string dateFormatString, CultureInfo culture, out object dt)
		{
			if (dateParseHandling == DateParseHandling.DateTimeOffset)
			{
				DateTimeOffset temp;
				if (DateTimeOffset.TryParseExact(text, dateFormatString, culture, DateTimeStyles.RoundtripKind, out temp))
				{
					dt = temp;
					return true;
				}
			}
			else
			{
				DateTime temp2;
				if (DateTime.TryParseExact(text, dateFormatString, culture, DateTimeStyles.RoundtripKind, out temp2))
				{
					temp2 = DateTimeUtils.EnsureDateTime(temp2, dateTimeZoneHandling);
					dt = temp2;
					return true;
				}
			}
			dt = null;
			return false;
		}
		private static TimeSpan ReadOffset(string offsetText)
		{
			bool negative = offsetText[0] == '-';
			int hours = int.Parse(offsetText.Substring(1, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
			int minutes = 0;
			if (offsetText.Length >= 5)
			{
				minutes = int.Parse(offsetText.Substring(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture);
			}
			TimeSpan offset = TimeSpan.FromHours((double)hours) + TimeSpan.FromMinutes((double)minutes);
			if (negative)
			{
				offset = offset.Negate();
			}
			return offset;
		}
		internal static void WriteDateTimeString(TextWriter writer, DateTime value, DateFormatHandling format, string formatString, CultureInfo culture)
		{
			if (string.IsNullOrEmpty(formatString))
			{
				char[] chars = new char[64];
				int pos = DateTimeUtils.WriteDateTimeString(chars, 0, value, null, value.Kind, format);
				writer.Write(chars, 0, pos);
				return;
			}
			writer.Write(value.ToString(formatString, culture));
		}
		internal static int WriteDateTimeString(char[] chars, int start, DateTime value, TimeSpan? offset, DateTimeKind kind, DateFormatHandling format)
		{
			int pos;
			if (format == DateFormatHandling.MicrosoftDateFormat)
			{
				TimeSpan o = offset ?? value.GetUtcOffset();
				long javaScriptTicks = DateTimeUtils.ConvertDateTimeToJavaScriptTicks(value, o);
				"\\/Date(".CopyTo(0, chars, start, 7);
				pos = start + 7;
				string ticksText = javaScriptTicks.ToString(CultureInfo.InvariantCulture);
				ticksText.CopyTo(0, chars, pos, ticksText.Length);
				pos += ticksText.Length;
				switch (kind)
				{
				case DateTimeKind.Unspecified:
					if (value != DateTime.MaxValue && value != DateTime.MinValue)
					{
						pos = DateTimeUtils.WriteDateTimeOffset(chars, pos, o, format);
					}
					break;
				case DateTimeKind.Local:
					pos = DateTimeUtils.WriteDateTimeOffset(chars, pos, o, format);
					break;
				}
				")\\/".CopyTo(0, chars, pos, 3);
				pos += 3;
			}
			else
			{
				pos = DateTimeUtils.WriteDefaultIsoDate(chars, start, value);
				switch (kind)
				{
				case DateTimeKind.Utc:
					chars[pos++] = 'Z';
					break;
				case DateTimeKind.Local:
					pos = DateTimeUtils.WriteDateTimeOffset(chars, pos, offset ?? value.GetUtcOffset(), format);
					break;
				}
			}
			return pos;
		}
		internal static int WriteDefaultIsoDate(char[] chars, int start, DateTime dt)
		{
			int length = 19;
			int year;
			int month;
			int day;
			DateTimeUtils.GetDateValues(dt, out year, out month, out day);
			DateTimeUtils.CopyIntToCharArray(chars, start, year, 4);
			chars[start + 4] = '-';
			DateTimeUtils.CopyIntToCharArray(chars, start + 5, month, 2);
			chars[start + 7] = '-';
			DateTimeUtils.CopyIntToCharArray(chars, start + 8, day, 2);
			chars[start + 10] = 'T';
			DateTimeUtils.CopyIntToCharArray(chars, start + 11, dt.Hour, 2);
			chars[start + 13] = ':';
			DateTimeUtils.CopyIntToCharArray(chars, start + 14, dt.Minute, 2);
			chars[start + 16] = ':';
			DateTimeUtils.CopyIntToCharArray(chars, start + 17, dt.Second, 2);
			int fraction = (int)(dt.Ticks % 10000000L);
			if (fraction != 0)
			{
				int digits = 7;
				while (fraction % 10 == 0)
				{
					digits--;
					fraction /= 10;
				}
				chars[start + 19] = '.';
				DateTimeUtils.CopyIntToCharArray(chars, start + 20, fraction, digits);
				length += digits + 1;
			}
			return start + length;
		}
		private static void CopyIntToCharArray(char[] chars, int start, int value, int digits)
		{
			while (digits-- != 0)
			{
				chars[start + digits] = (char)(value % 10 + 48);
				value /= 10;
			}
		}
		internal static int WriteDateTimeOffset(char[] chars, int start, TimeSpan offset, DateFormatHandling format)
		{
			chars[start++] = ((offset.Ticks >= 0L) ? '+' : '-');
			int absHours = Math.Abs(offset.Hours);
			DateTimeUtils.CopyIntToCharArray(chars, start, absHours, 2);
			start += 2;
			if (format == DateFormatHandling.IsoDateFormat)
			{
				chars[start++] = ':';
			}
			int absMinutes = Math.Abs(offset.Minutes);
			DateTimeUtils.CopyIntToCharArray(chars, start, absMinutes, 2);
			start += 2;
			return start;
		}
		internal static void WriteDateTimeOffsetString(TextWriter writer, DateTimeOffset value, DateFormatHandling format, string formatString, CultureInfo culture)
		{
			if (string.IsNullOrEmpty(formatString))
			{
				char[] chars = new char[64];
				int pos = DateTimeUtils.WriteDateTimeString(chars, 0, (format == DateFormatHandling.IsoDateFormat) ? value.DateTime : value.UtcDateTime, new TimeSpan?(value.Offset), DateTimeKind.Local, format);
				writer.Write(chars, 0, pos);
				return;
			}
			writer.Write(value.ToString(formatString, culture));
		}
		private static void GetDateValues(DateTime td, out int year, out int month, out int day)
		{
			long ticks = td.Ticks;
			int i = (int)(ticks / 864000000000L);
			int y400 = i / 146097;
			i -= y400 * 146097;
			int y401 = i / 36524;
			if (y401 == 4)
			{
				y401 = 3;
			}
			i -= y401 * 36524;
			int y402 = i / 1461;
			i -= y402 * 1461;
			int y403 = i / 365;
			if (y403 == 4)
			{
				y403 = 3;
			}
			year = y400 * 400 + y401 * 100 + y402 * 4 + y403 + 1;
			i -= y403 * 365;
			int[] days = (y403 == 3 && (y402 != 24 || y401 == 3)) ? DateTimeUtils.DaysToMonth366 : DateTimeUtils.DaysToMonth365;
			int j = i >> 6;
			while (i >= days[j])
			{
				j++;
			}
			month = j;
			day = i - days[j - 1] + 1;
		}
	}
}
