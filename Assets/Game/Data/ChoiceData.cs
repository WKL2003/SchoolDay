using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Choice", fileName = "Choice")]
    public sealed class ChoiceData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)] public string PromptLine;
        public float Cost;
        public StatDelta Delta;
        public LeakTag LeakTag;
        public OverspendRule Overspend;
        public Sprite Icon;
        public Sprite Backdrop;
        public string PromoGroup;
    }
}
