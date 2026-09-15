using System;

namespace SchoolDay
{
    public static class WeekEconomy
    {
        public static MoneyStats Morning(DayConfig day, float leftoverPocket)
        {
            return Morning(day, leftoverPocket, null);
        }

        public static MoneyStats Morning(DayConfig day, float leftoverPocket, MoneyStats leftover)
        {
            if (day == null)
                throw new ArgumentNullException(nameof(day));

            MoneyStats stats = day.CreateOpeningStats();
            float pocket = leftoverPocket + day.StartingPocket;
            stats.Pocket = pocket;
            stats.Buffer = pocket;
            if (leftover != null)
            {
                stats.Fuel = leftover.Fuel - day.OvernightFuelDrop;
                stats.Face = leftover.Face;
            }
            day.Clamp(stats);
            return stats;
        }
    }
}
