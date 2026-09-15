using System;

namespace SchoolDay
{
    public static class WeekCalendar
    {
        public static SchoolWeekday Resolve(WeekBoard board, DateTime now)
        {
            if (board == null)
                return SchoolWeekday.Monday;

            if (board.UsePreviewWeekday)
                return board.PreviewWeekday;

            return FromDate(now, board.WeekendFallback);
        }

        public static SchoolWeekday FromDate(DateTime now, SchoolWeekday weekendFallback)
        {
            DayOfWeek day = now.DayOfWeek;
            if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
                return weekendFallback;

            return (SchoolWeekday)(int)day;
        }

        public static SchoolWeekday Next(SchoolWeekday weekday)
        {
            if (weekday >= SchoolWeekday.Friday)
                return SchoolWeekday.Monday;

            return weekday + 1;
        }

        public static int MixSeed(int weekSeed, SchoolWeekday weekday, DateTime date, int nonce)
        {
            return MixSeed(weekSeed, weekday, date, nonce, 0);
        }

        public static int MixSeed(int weekSeed, SchoolWeekday weekday, DateTime date, int nonce, int playNonce)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + weekSeed;
                hash = hash * 31 + (int)weekday;
                hash = hash * 31 + date.Year;
                hash = hash * 31 + date.Month;
                hash = hash * 31 + date.Day;
                hash = hash * 31 + nonce;
                hash = hash * 31 + playNonce;
                if (hash == int.MinValue)
                    return 1;
                return hash == 0 ? 1 : hash;
            }
        }
    }
}
