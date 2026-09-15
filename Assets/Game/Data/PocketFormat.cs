using System;
using System.Globalization;

namespace SchoolDay
{
    public static class PocketFormat
    {
        public static string Cash(float amount)
        {
            if (amount > -0.001f && amount < 0.001f)
                amount = 0f;

            float abs = Math.Abs(amount);
            string number = Math.Abs(abs - Math.Round(abs)) < 0.001f
                ? Math.Round(abs).ToString("0", CultureInfo.InvariantCulture)
                : abs.ToString("0.00", CultureInfo.InvariantCulture);

            if (amount < 0f)
                return "\u2212$" + number;

            return "$" + number;
        }

        public static string PriceDelta(float cost)
        {
            if (cost <= 0.001f)
                return "—" + Cash(0f);

            return "—" + Cash(cost);
        }
    }
}
