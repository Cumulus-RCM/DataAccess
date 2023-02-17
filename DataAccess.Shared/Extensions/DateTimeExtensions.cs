using System.Globalization;

namespace DataAccess.Shared {
   public static class DateTimeExtensions {
        public static int ToAge(this DateTime date) {
            if (date == DateTime.MinValue) return 0;
            return DateTime.Now.DayOfYear < date.DayOfYear
                ? DateTime.Now.Year - date.Year - 1
                : DateTime.Now.Year - date.Year;
        }

        public static DateTime ToNextSaturday(this DateTime value) {
            var delta = DayOfWeek.Saturday - value.DayOfWeek;
            if (delta == 0) delta = 7; 
            return value.AddDays(delta);
        }

        public static DateTime ToNextOrToday(this DateTime value, DayOfWeek day) {
            return value.DayOfWeek == day
                ? value
                : day > value.DayOfWeek
                    ? value.AddDays(day - value.DayOfWeek)
                    : value.AddDays(7 - (value.DayOfWeek - day));
        }

        public static DateTime Midnight(this DateTime value) => value.Date.AddSeconds(86399);

        public static int DayOfWeekDifference(this DateTime value, DayOfWeek day) {
            var difference = day - value.DayOfWeek;
            return difference >= 0 ? difference : difference + 7;
        }

        public static string ToLongDateShortTimeString(this DateTime value, bool newLineSeparator = false) {
            var separator = newLineSeparator ? Environment.NewLine : " ";
            return value.ToString("D", CultureInfo.CurrentCulture) + separator + value.ToString("t", CultureInfo.CurrentCulture);
        }

        public static string ToLongDateShortTimeString(this DateTime? value, bool newLineSeparator = false) =>
            value.HasValue ? ToLongDateShortTimeString(value.Value, newLineSeparator) : string.Empty;

        public static string ToShortDateTimeString(this DateTime value, bool newLineSeparator = false) => newLineSeparator
            ? value.ToString("d", CultureInfo.CurrentCulture) + Environment.NewLine + value.ToString("t", CultureInfo.CurrentCulture)
            : value.ToString("g", CultureInfo.CurrentCulture);

        public static string ToShortDateTimeString(this DateTime? value, bool newLineSeparator = false) =>
            value.HasValue ? ToShortDateTimeString(value.Value, newLineSeparator) : string.Empty;

        public static string ToShortDateString(this DateTime value) => value.ToString("d", CultureInfo.CurrentCulture);

        public static string ToShortDateString(this DateTime? value) => value?.ToShortDateString() ?? string.Empty;
        public static DateTime? ToDateTime(this string dateString) { return DateTime.TryParse(dateString, out var date) ? date : null; }
    }

}