using System;

namespace SchoolDay
{
    public readonly struct WakeTime
    {
        public const int DefaultEarliestMinute = 6 * 60 + 10;
        public const int DefaultLatestMinute = 6 * 60 + 55;

        public WakeTime(int hour, int minute)
        {
            Hour = hour;
            Minute = minute;
        }

        public int Hour { get; }
        public int Minute { get; }
        public string ClockDigits => string.Format("{0:00}:{1:00}", Hour, Minute);
        public string SpeakerLabel => string.Format("{0}:{1:00}am", Hour, Minute);

        public static WakeTime Pick(Random rng, DayConfig day)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            int from = day != null ? day.WakeEarliestMinute : DefaultEarliestMinute;
            int to = day != null ? day.WakeLatestMinute : DefaultLatestMinute;
            if (to < from)
                to = from;

            int picked = rng.Next(from, to + 1);
            return new WakeTime(picked / 60, picked % 60);
        }
    }
}
