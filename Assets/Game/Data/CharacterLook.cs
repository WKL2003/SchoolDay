using UnityEngine;
using UnityEngine.Serialization;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Character Look", fileName = "CharacterLook")]
    public sealed class CharacterLook : ScriptableObject
    {
        public string DisplayName;
        public Sprite Idle;

        [Tooltip("Small bust for the HUD dashboard. Uses Idle if empty.")]
        public Sprite HudFace;

        [FormerlySerializedAs("Tired")]
        [Tooltip("Shown when Energy is low.")]
        public Sprite LowEnergy;

        [FormerlySerializedAs("Paiseh")]
        [Tooltip("Shown when Reputation is low.")]
        public Sprite LowReputation;

        [FormerlySerializedAs("Happy")]
        [Tooltip("Shown when Energy and Reputation are both high.")]
        public Sprite HighEnergyReputation;

        public Sprite[] WaveFrames;

        [Tooltip("Standing breath loop (3 frames: 0 -> 1 -> 2 -> 1 -> 0)")]
        public Sprite[] IdleFrames;

        [Tooltip("Sleepy head nod loop (3 frames: 0 -> 1 -> 2 -> 1 -> 0)")]
        public Sprite[] LowEnergyFrames;

        [Tooltip("Finger fidget loop (3 frames: 0 -> 1 -> 2 -> 1 -> 0)")]
        public Sprite[] LowReputationFrames;

        [Tooltip("Tiny bounce loop (3 frames: 0 -> 1 -> 2 -> 1 -> 0)")]
        public Sprite[] HighEnergyReputationFrames;
    }
}
