using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GCL
{
    class Converters
    {
        public class StringHelper
        {
            static public String CleanString(String str)
            {
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                return rgx.Replace(str, "_");
            }
        }
        public class RelativeDateValue : IValueConverter
        {
            public object           Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var v = value as DateTime?;
                if (v == null)
                {
                    return value;
                }

                return Convert(v.Value);
            }

            public static int       Compare(DateTime a, DateTime b)
            {
                return Convert(a) == Convert(b) ? 0 : a.CompareTo(b);
            }
            public static string    Convert(DateTime v)
            {
                var date = v.Date;
                var today = DateTime.Today;
                var diff = today - date;

                if (date > today)
                    return date.ToString("yyyy - MMMM") + " [the future]";

                if (diff.Days == 0)
                    return "Today";
                if (diff.Days == 1)
                    return "Yesterday";
                if (diff.Days < 7)
                    return date.DayOfWeek.ToString();
                if (diff.Days < 14)
                    return "Last week";
                if (date.Year == today.Year && date.Month == today.Month)
                    return "This month";

                var lastMonth = today.AddMonths(-1);
                if (date.Year == lastMonth.Year && date.Month == lastMonth.Month)
                    return "Last month";
                if (date.Year == today.Year)
                    return date.ToString("yyyy - MMMM"); // return "This year";

                return date.Year.ToString();
            }
            public object           ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
