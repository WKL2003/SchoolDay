using System;
using System.Collections.Generic;

namespace SchoolDay
{
    public static class PromoRules
    {
        public static PromoData[] PickActive(PromoCatalog catalog, SchoolWeekday weekday, Random rng)
        {
            if (catalog == null || catalog.Items == null || catalog.Items.Length == 0 || rng == null)
                return Array.Empty<PromoData>();

            var fired = new List<PromoData>(catalog.Items.Length);
            int[] order = ShuffledOrder(catalog.Items.Length, rng);
            for (int i = 0; i < order.Length; i++)
            {
                PromoData promo = catalog.Items[order[i]];
                if (!HasGroup(promo))
                    continue;
                if (!WeekdayOk(promo, weekday))
                    continue;
                if (rng.NextDouble() >= ChanceOf(promo))
                    continue;

                fired.Add(promo);
            }

            int cap = catalog.MaxActive;
            if (cap > 0 && fired.Count > cap)
            {
                Shuffle(fired, rng);
                fired.RemoveRange(cap, fired.Count - cap);
            }

            return fired.ToArray();
        }

        public static PromoBias BiasFor(PromoCatalog catalog, PromoData[] active)
        {
            int weight = catalog != null ? catalog.BoostWeight : 1;
            return new PromoBias(GroupsOf(active), weight);
        }

        public static HashSet<string> GroupsOf(PromoData[] active)
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (active == null)
                return groups;

            for (int i = 0; i < active.Length; i++)
            {
                string group = GroupOf(active[i]);
                if (!string.IsNullOrEmpty(group))
                    groups.Add(group);
            }

            return groups;
        }

        public static AppliedPromo[] Apply(ChoiceData[] choices, PromoData[] active, SchoolWeekday weekday, PromoCatalog catalog)
        {
            if (choices == null || choices.Length == 0)
                return Array.Empty<AppliedPromo>();

            var overlays = new AppliedPromo[choices.Length];
            for (int i = 0; i < choices.Length; i++)
                overlays[i] = Apply(choices[i], active, weekday, catalog);

            return overlays;
        }

        public static AppliedPromo Apply(ChoiceData choice, PromoData[] active, SchoolWeekday weekday, PromoCatalog catalog)
        {
            if (choice == null || active == null || !HasGroup(choice) || choice.Cost <= 0.001f)
                return AppliedPromo.None;

            for (int i = 0; i < active.Length; i++)
            {
                PromoData promo = active[i];
                if (!Matches(promo, choice, weekday))
                    continue;

                float paid = CutCost(promo, choice.Cost);
                if (paid + 0.001f >= choice.Cost)
                    continue;

                string prompt = !string.IsNullOrEmpty(promo.PromptLine) ? promo.PromptLine : choice.PromptLine;
                return new AppliedPromo(
                    promo,
                    choice.Cost,
                    paid,
                    prompt,
                    BadgeOf(promo, catalog),
                    ExpenseFormatOf(catalog));
            }

            return AppliedPromo.None;
        }

        public static float PaidCost(ChoiceData choice, AppliedPromo promo)
        {
            if (promo.Active)
                return promo.PaidCost;
            return choice != null ? choice.Cost : 0f;
        }

        public static string ExpenseLabel(ChoiceData choice, AppliedPromo promo)
        {
            string name = choice != null ? choice.DisplayName : "";
            if (!promo.Active || string.IsNullOrEmpty(promo.PromoName))
                return name;
            if (string.IsNullOrEmpty(promo.ExpenseFormat))
                return name;
            if (string.IsNullOrEmpty(name))
                return promo.PromoName;
            return string.Format(promo.ExpenseFormat, name, promo.PromoName);
        }

        public static string PriceLabel(string cash, AppliedPromo promo)
        {
            if (string.IsNullOrEmpty(cash) || !promo.Active || string.IsNullOrEmpty(promo.Badge))
                return cash ?? "";
            if (promo.Badge.IndexOf("{0}", StringComparison.Ordinal) >= 0)
                return string.Format(promo.Badge, cash);
            return promo.Badge + " " + cash;
        }

        public static bool Matches(PromoData promo, ChoiceData choice, SchoolWeekday weekday)
        {
            if (promo == null || choice == null)
                return false;
            if (!SameGroup(GroupOf(promo), GroupOf(choice)))
                return false;
            if (!WeekdayOk(promo, weekday))
                return false;
            if (promo.UseCostRange && !InCostRange(promo, choice.Cost))
                return false;

            return choice.Cost > 0.001f;
        }

        public static bool HasGroup(ChoiceData choice)
        {
            return choice != null && !string.IsNullOrEmpty(GroupOf(choice));
        }

        public static bool HasGroup(PromoData promo)
        {
            return promo != null && !string.IsNullOrEmpty(GroupOf(promo));
        }

        public static string GroupOf(ChoiceData choice)
        {
            return choice != null ? Normalize(choice.PromoGroup) : "";
        }

        public static string GroupOf(PromoData promo)
        {
            return promo != null ? Normalize(promo.Group) : "";
        }

        public static bool SameGroup(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return false;
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        static string BadgeOf(PromoData promo, PromoCatalog catalog)
        {
            if (promo != null && !string.IsNullOrEmpty(promo.Badge))
                return promo.Badge;
            if (catalog != null && !string.IsNullOrEmpty(catalog.Badge))
                return catalog.Badge;
            return "";
        }

        static string ExpenseFormatOf(PromoCatalog catalog)
        {
            return catalog != null ? catalog.ExpenseFormat ?? "" : "";
        }

        static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Trim();
        }

        static bool WeekdayOk(PromoData promo, SchoolWeekday weekday)
        {
            if (promo == null || !promo.UseWeekdays || promo.Weekdays == null || promo.Weekdays.Length == 0)
                return true;

            for (int i = 0; i < promo.Weekdays.Length; i++)
            {
                if (promo.Weekdays[i] == weekday)
                    return true;
            }

            return false;
        }

        static bool InCostRange(PromoData promo, float cost)
        {
            const float epsilon = 0.001f;
            return cost + epsilon >= promo.MinCost && cost - epsilon <= promo.MaxCost;
        }

        static float ChanceOf(PromoData promo)
        {
            if (promo == null)
                return 0f;
            if (promo.Chance < 0f)
                return 0f;
            if (promo.Chance > 1f)
                return 1f;
            return promo.Chance;
        }

        static float CutCost(PromoData promo, float list)
        {
            float paid;
            switch (promo.Cut)
            {
                case PromoCut.Percent:
                    paid = list * (1f - promo.CutValue);
                    break;
                case PromoCut.Amount:
                    paid = list - promo.CutValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(promo.Cut), promo.Cut, null);
            }

            float floor = promo.MinPaid > 0f ? promo.MinPaid : 0f;
            if (paid < floor)
                paid = floor;
            if (paid < 0f)
                paid = 0f;

            return (float)Math.Round(paid * 100d) / 100f;
        }

        static int[] ShuffledOrder(int length, Random rng)
        {
            var order = new int[length];
            for (int i = 0; i < length; i++)
                order[i] = i;
            for (int i = length - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                int swap = order[i];
                order[i] = order[j];
                order[j] = swap;
            }

            return order;
        }

        static void Shuffle(List<PromoData> items, Random rng)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                PromoData swap = items[i];
                items[i] = items[j];
                items[j] = swap;
            }
        }
    }
}
