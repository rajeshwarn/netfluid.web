using System;
using System.Collections.Generic;
using System.Linq;

namespace NetFluid.Cron
{
    public static class Cron
    {
        static int TextToValue(string text)
        {
            if (char.IsLetter(text[0]))
            {
                switch (text)
                {
                    case "JAN": return 1;
                    case "FEB": return 2;
                    case "MAR": return 3;
                    case "APR": return 4;
                    case "MAY": return 5;
                    case "JUN": return 6;
                    case "JUL": return 7;
                    case "AUG": return 8;
                    case "SEP": return 9;
                    case "OCT": return 10;
                    case "NOV": return 11;
                    case "DEC": return 12;

                    case "SUN": return 0;
                    case "MON": return 1;
                    case "TUE": return 2;
                    case "WED": return 3;
                    case "THU": return 4;
                    case "FRI": return 5;
                    case "SAT": return 6;
                }
            }
            return int.Parse(text);
        }

        static int[] Parse(string val,IEnumerable<int> range)
        {
            var step = 0;
            var slashIndex = val.IndexOf('/');
            if (slashIndex >= 0)
            {
                step = int.Parse(val.Substring(slashIndex + 1));
                val = val.Substring(0, slashIndex);
            }

            if (val=="*")
                return range.ToArray();

            var parts = val.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            var res = parts.SelectMany(x =>
            {
                var index = x.IndexOf('-');
                if (index >= 0)
                {
                    var f = TextToValue(x.Substring(0, index));
                    var s = TextToValue(x.Substring(index + 1));

                    return step != 0 ? Enumerable.Range(f, s).Where(y => y % step == 0) : Enumerable.Range(f, s);
                }
                return new[]{TextToValue(x)};
            }).OrderBy(x=>x).ToArray();

            return res;
        }

        static int GetAndShift(int[] from, ref int[] to, int refer, int refer2,ref bool carry)
        {
            var m = carry ? from.Where(x=> x<= refer) : from.Where(x => x >= refer);

            int result;

            if (m.Any())
            {
                if (carry)
                {
                    var shift = Array.IndexOf(to, refer2) + 1;

                    if (to.Count() > shift)
                    {
                        to = to.Skip(shift).ToArray();
                    }
                    carry = false;
                }
                result = m.Min();
            }
            else
            {
                result = from.Min();
                var shift = Array.IndexOf(to, refer2) + 1;
                
                if (to.Count()>shift)
                {
                    to = to.Skip(shift).ToArray();
                }
                else
                {
                    carry = true;
                }
            }
            return result;
        }

        public static DateTime Next(string cron)
        {
            return Next(cron, DateTime.Now);
        }
        public static DateTime Next(string cron, DateTime from)
        {
            var parts = cron.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

            top:
            var years = Parse(parts[5], Enumerable.Range(2014, 250));
            var months = Parse(parts[3], Enumerable.Range(1, 12));
            var days = Parse(parts[2], Enumerable.Range(1, 31));
            var hours = Parse(parts[1], Enumerable.Range(0, 24));
            var minutes = Parse(parts[0], Enumerable.Range(0, 60));

            bool carry=false;
            var min = GetAndShift(minutes,ref hours, from.Minute,from.Hour,ref carry);
            var hou = GetAndShift(hours, ref days, from.Hour, from.Day, ref carry);
            var day = GetAndShift(days, ref months, from.Day, from.Month, ref carry);
            var mon = GetAndShift(months, ref years, from.Month, from.Year, ref carry);
            var yea = years.Min();

            //adjustment
            if (day > DateTime.DaysInMonth(yea,mon))
            {
                mon = mon == 12 ? 1 : mon+1;
                day = 1;
            }

            var dt = new DateTime(yea, mon, day, hou, min, 0);

            if (dt.Equals(from))
            {
                from = from + TimeSpan.FromMinutes(1);
                goto top;
            }

            var dweek = Parse(parts[4], Enumerable.Range(0, 7));
            if (!dweek.Contains((int) dt.DayOfWeek))
            {
                from = from + TimeSpan.FromDays(1);
                goto top;
            }

            return dt;
        }

        private static readonly List<CronTask> tasks;

        static Cron()
        {
            tasks = new List<CronTask>();
        }

        public static void AddTask(string cron, Action action,Action onComplete=null,Action<Exception> onError=null)
        {
            lock (tasks)
            {
                tasks.Add(new CronTask(cron,action,onComplete,onError));
            }
        }

        public static void AddTask(string cron,DateTime from, Action action, Action onComplete = null, Action<Exception> onError = null)
        {
            lock (tasks)
            {
                tasks.Add(new CronTask(cron,from, action, onComplete, onError));
            }
        }
    }
}
