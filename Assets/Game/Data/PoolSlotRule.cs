using System;

namespace SchoolDay
{
    [Serializable]
    public struct PoolSlotRule
    {
        public string Label;
        public bool UseCostRange;
        public float MinCost;
        public float MaxCost;
        public bool RequireLateRisk;
        public bool RequireNegativeFace;
        public bool UseLeak;
        public LeakTag Leak;
        public bool UseOverspend;
        public OverspendRule Overspend;

        public bool Matches(ChoiceData choice)
        {
            if (choice == null)
                return false;

            if (UseCostRange && !InCostRange(choice.Cost))
                return false;
            if (RequireLateRisk && choice.Delta.LateRisk <= 0f)
                return false;
            if (RequireNegativeFace && choice.Delta.FaceChange >= 0f)
                return false;
            if (UseLeak && choice.LeakTag != Leak)
                return false;
            if (UseOverspend && choice.Overspend != Overspend)
                return false;

            return true;
        }

        bool InCostRange(float cost)
        {
            const float epsilon = 0.001f;
            return cost + epsilon >= MinCost && cost - epsilon <= MaxCost;
        }
    }
}
