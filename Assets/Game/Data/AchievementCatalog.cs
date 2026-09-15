using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Achievement Catalog", fileName = "AchievementCatalog")]
    public sealed class AchievementCatalog : ScriptableObject
    {
        public AchievementData[] Items;
    }
}
