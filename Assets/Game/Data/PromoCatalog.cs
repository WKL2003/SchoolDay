using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Promo Catalog", fileName = "PromoCatalog")]
    public sealed class PromoCatalog : ScriptableObject
    {
        public PromoData[] Items;
        public int MaxActive = 2;
        public int BoostWeight = 3;
        public string Badge = "PROMO";
        public string ExpenseFormat = "{0} · {1}";
    }
}
