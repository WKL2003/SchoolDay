using System;

namespace SchoolDay
{
    [Serializable]
    public sealed class MoneyStats
    {
        public float Pocket;
        public float Fuel;
        public float Face;
        public float Buffer;

        public bool CanAfford(float cost)
        {
            return Pocket + 0.001f >= cost;
        }

        public void Apply(in StatDelta delta)
        {
            Pocket -= delta.PocketCost;
            Fuel += delta.FuelChange;
            Face += delta.FaceChange;
            Buffer += delta.BufferChange;
        }

        public MoneyStats Clone()
        {
            return new MoneyStats
            {
                Pocket = Pocket,
                Fuel = Fuel,
                Face = Face,
                Buffer = Buffer
            };
        }
    }
}
