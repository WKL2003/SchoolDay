using UnityEngine;

namespace SchoolDay
{
    public static class RuleBook
    {
        public static float ExtraFuel(ChoiceData choice, MoneyStats statsBefore, DayConfig config)
        {
            if (choice.Delta.LateRisk <= 0f)
                return 0f;
            if (statsBefore.Fuel > config.LowFuelThreshold)
                return 0f;

            return config.WalkLowFuelExtraHit;
        }

        public static float DebtReputation(float pocketBefore, float pocketAfter, DayConfig config)
        {
            if (config == null || config.DebtReputationPerDollar <= 0.001f)
                return 0f;

            float debtBefore = Mathf.Max(0f, -pocketBefore);
            float debtAfter = Mathf.Max(0f, -pocketAfter);
            float added = debtAfter - debtBefore;
            if (added <= 0.001f)
                return 0f;

            return -added * config.DebtReputationPerDollar;
        }
    }
}
