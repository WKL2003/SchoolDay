using System;

namespace SchoolDay
{
    [Serializable]
    public struct CoachLine
    {
        public SpeakerKind Speaker;
        public LeakTag WhenLeak;
        public bool RequireBufferKid;
        public bool RequireBroke;
        public bool RequireFuelMiss;
        public string Line;
    }
}
