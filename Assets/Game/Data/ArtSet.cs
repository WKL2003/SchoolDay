using UnityEngine;
using UnityEngine.Serialization;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Art Set", fileName = "ArtSet")]
    public sealed class ArtSet : ScriptableObject
    {
        public Sprite HudPanel;
        public Sprite BadgeBufferKid;
        public Sprite ButtonNormal;
        public Sprite ButtonConfirm;
        public Color Text = new Color(0.88f, 0.86f, 0.82f);
        public Color Muted = new Color(0.60f, 0.59f, 0.56f);
        public Color PriceOk = new Color(0.78f, 0.68f, 0.42f);
        public Color PriceBad = new Color(0.72f, 0.42f, 0.38f);
        [FormerlySerializedAs("Fuel")]
        public Color Energy = new Color(0.40f, 0.55f, 0.46f);
        [FormerlySerializedAs("Face")]
        public Color Reputation = new Color(0.48f, 0.58f, 0.66f);
        public Color Buffer = new Color(0.70f, 0.60f, 0.40f);
        public Color Panel = new Color(0.12f, 0.14f, 0.16f, 0.90f);
        [FormerlySerializedAs("TiredFuelMax")]
        public float LowEnergyMax = 40f;
        [FormerlySerializedAs("PaisehFaceMax")]
        public float LowReputationMax = 45f;
        [FormerlySerializedAs("HappyFaceMin")]
        public float HighReputationMin = 70f;
        [FormerlySerializedAs("HappyFuelMin")]
        public float HighEnergyMin = 50f;

        public Sprite SpriteFor(MoneyStats stats, CharacterLook look)
        {
            return SpriteFor(stats, look, stats != null ? stats.Face : 0f);
        }

        public Sprite SpriteFor(MoneyStats stats, CharacterLook look, float standing)
        {
            if (look == null)
                return null;
            if (stats != null && stats.Fuel <= LowEnergyMax)
                return look.LowEnergy != null ? look.LowEnergy : look.Idle;
            if (stats != null && standing <= LowReputationMax)
                return look.LowReputation != null ? look.LowReputation : look.Idle;
            if (stats != null && standing >= HighReputationMin && stats.Fuel >= HighEnergyMin)
                return look.HighEnergyReputation != null ? look.HighEnergyReputation : look.Idle;
            return look.Idle;
        }
    }
}
