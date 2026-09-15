using System;

namespace SchoolDay
{
    [Serializable]
    public struct StatDelta
    {
        public float PocketCost;
        public float FuelChange;
        public float FaceChange;
        public float BufferChange;
        public float LateRisk;
        public LeakTag MarksLeak;
    }
}
