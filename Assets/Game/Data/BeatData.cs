using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Beat", fileName = "Beat")]
    public sealed class BeatData : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea(2, 5)] public string BodyLine;
        public SpeakerKind Speaker;
        public BeatPhase Phase;
        public Sprite Background;
        public ChoiceData[] Choices;
    }
}
