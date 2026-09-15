using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Promo", fileName = "Promo")]
    public sealed class PromoData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)] public string BannerLine;
        [TextArea(2, 4)] public string PromptLine;
        public string Group;
        public string Badge;
        [Range(0f, 1f)] public float Chance = 0.25f;
        public PromoCut Cut;
        public float CutValue = 0.2f;
        public float MinPaid = 0.5f;
        public bool UseCostRange;
        public float MinCost;
        public float MaxCost;
        public bool UseWeekdays;
        public SchoolWeekday[] Weekdays;
    }
}
