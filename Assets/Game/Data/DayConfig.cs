using System;
using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Day Config", fileName = "Monday")]
    public sealed class DayConfig : ScriptableObject
    {
        public float StartingPocket = 8f;
        public float StartingFuel = 70f;
        public float StartingFace = 55f;
        public float StartingBuffer = 8f;
        public float OvernightFuelDrop = 20f;
        public float FuelMetThreshold = 45f;
        public float BufferWin = 2f;
        public float StatFloor;
        public float StatCap = 100f;
        public float LowFuelThreshold = 30f;
        public float WalkLowFuelExtraHit = -6f;
        public float DebtReputationPerDollar = 2f;
        public float DebtReputationZeroAt = 8f;
        public float ShockSkipFaceHit = 12f;
        public float ShockPayFaceGain = 6f;
        public float ReputationPunctualWeight = 1f;
        public float ReputationFriendlyWeight = 1f;
        public float ReputationAdaptiveWeight = 1f;
        public float ReputationGenerousWeight = 1f;
        public float ReputationLeakKeepScale = 0.75f;
        public float ReputationGenerousPending = 50f;
        public int ShockSeed;
        public LeakTag[] LeakPriority = { LeakTag.Grab, LeakTag.Drinks, LeakTag.SkipMeal };
        public BeatData[] Beats;
        public CoachLine[] CoachLines;
        public string GrabLeakName = "Grab";
        public string DrinksLeakName = "drinks";
        public string SkipMealLeakName = "skip meal";
        public string NoneLeakName = "none";
        public string AuntieName = "Canteen auntie";
        public string FriendName = "Your friend";
        public string MumName = "Mum";
        public string TeacherName = "Form teacher";
        public string TitleSpeakerName = "6:40am";
        public int WakeEarliestMinute = WakeTime.DefaultEarliestMinute;
        public int WakeLatestMinute = WakeTime.DefaultLatestMinute;
        public string BudgetTip = "Budget";
        public string EnergyTip = "Energy";
        public string ReputationTip = "Reputation";
        public string KeepTip = "Keep $2";

        public MoneyStats CreateOpeningStats()
        {
            return new MoneyStats
            {
                Pocket = StartingPocket,
                Fuel = StartingFuel,
                Face = StartingFace,
                Buffer = StartingBuffer
            };
        }

        public void Clamp(MoneyStats stats)
        {
            stats.Fuel = Mathf.Clamp(stats.Fuel, StatFloor, StatCap);
            stats.Face = Mathf.Clamp(stats.Face, StatFloor, StatCap);
        }

        public string SpeakerName(SpeakerKind speaker)
        {
            switch (speaker)
            {
                case SpeakerKind.Title: return TitleSpeakerName;
                case SpeakerKind.Auntie: return AuntieName;
                case SpeakerKind.Friend: return FriendName;
                case SpeakerKind.Mum: return MumName;
                case SpeakerKind.Teacher: return TeacherName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(speaker), speaker, null);
            }
        }

        public string LeakName(LeakTag tag)
        {
            switch (tag)
            {
                case LeakTag.None: return NoneLeakName;
                case LeakTag.Grab: return GrabLeakName;
                case LeakTag.Drinks: return DrinksLeakName;
                case LeakTag.SkipMeal: return SkipMealLeakName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
            }
        }

        public CoachLine PickCoachLine(LeakTag leak, bool bufferKid, bool broke, bool fuelMiss)
        {
            int bestScore = -1;
            CoachLine best = default;
            bool found = false;

            for (int i = 0; i < CoachLines.Length; i++)
            {
                CoachLine line = CoachLines[i];
                if (string.IsNullOrEmpty(line.Line))
                    continue;
                if (line.WhenLeak != LeakTag.None && line.WhenLeak != leak)
                    continue;
                if (line.RequireBufferKid && !bufferKid)
                    continue;
                if (line.RequireBroke && !broke)
                    continue;
                if (line.RequireFuelMiss && !fuelMiss)
                    continue;

                int score = 0;
                if (line.WhenLeak == leak && leak != LeakTag.None)
                    score += 4;
                if (line.RequireBufferKid)
                    score += 2;
                if (line.RequireBroke)
                    score += 2;
                if (line.RequireFuelMiss)
                    score += 2;

                if (!found || score > bestScore)
                {
                    found = true;
                    bestScore = score;
                    best = line;
                }
            }

            return best;
        }
    }
}
