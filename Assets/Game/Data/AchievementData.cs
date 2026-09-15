using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Achievement", fileName = "Achievement")]
    public sealed class AchievementData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)] public string Description;
        public Sprite Icon;
        public ChoiceData ShockChoice;
        public string ChoiceId;
        public int SortOrder;
        public bool UnlockOnBufferKid;
    }
}
