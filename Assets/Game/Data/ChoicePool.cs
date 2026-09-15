using System;
using System.Collections.Generic;
using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Choice Pool", fileName = "ChoicePool")]
    public sealed class ChoicePool : ScriptableObject
    {
        public ChoiceData[] Items;
        public PoolSlotRule[] Slots;
        public bool ShuffleOrder = true;

        public ChoiceData[] PickThree(System.Random rng, HashSet<string> usedIds)
        {
            return PickThree(rng, usedIds, PromoBias.None);
        }

        public ChoiceData[] PickThree(System.Random rng, HashSet<string> usedIds, PromoBias bias)
        {
            if (Slots == null || Slots.Length != 3)
                throw new InvalidOperationException(name + " needs exactly 3 slot rules.");

            return PickSlots(rng, usedIds, bias);
        }

        public ChoiceData PickOne(System.Random rng, HashSet<string> usedIds)
        {
            return PickOne(rng, usedIds, PromoBias.None);
        }

        public ChoiceData PickOne(System.Random rng, HashSet<string> usedIds, PromoBias bias)
        {
            if (Slots == null || Slots.Length < 1)
                throw new InvalidOperationException(name + " needs a slot rule.");

            ChoiceData[] picked = PickSlots(rng, usedIds, bias);
            return picked[0];
        }

        ChoiceData[] PickSlots(System.Random rng, HashSet<string> usedIds, PromoBias bias)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            ChoiceData[] picked = new ChoiceData[Slots.Length];
            for (int i = 0; i < Slots.Length; i++)
            {
                picked[i] = PickMatching(Slots[i], rng, usedIds, bias);
                Remember(usedIds, picked[i]);
            }

            if (ShuffleOrder && picked.Length > 1)
                Shuffle(picked, rng);

            return picked;
        }

        ChoiceData PickMatching(PoolSlotRule slot, System.Random rng, HashSet<string> usedIds, PromoBias bias)
        {
            int total = 0;
            if (Items != null)
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    if (IsCandidate(Items[i], slot, usedIds))
                        total += Weight(Items[i], bias);
                }
            }

            if (total == 0)
            {
                string label = string.IsNullOrEmpty(slot.Label) ? "slot" : slot.Label;
                throw new InvalidOperationException(name + " has no unused choice for " + label + ".");
            }

            int pick = rng.Next(0, total);
            int walk = 0;
            for (int i = 0; i < Items.Length; i++)
            {
                if (!IsCandidate(Items[i], slot, usedIds))
                    continue;
                walk += Weight(Items[i], bias);
                if (pick < walk)
                    return Items[i];
            }

            throw new InvalidOperationException(name + " failed to pick a choice.");
        }

        static int Weight(ChoiceData choice, PromoBias bias)
        {
            string group = PromoRules.GroupOf(choice);
            if (!string.IsNullOrEmpty(group)
                && bias.Groups != null
                && bias.Groups.Contains(group))
                return bias.Weight;

            return 1;
        }

        static bool IsCandidate(ChoiceData choice, PoolSlotRule slot, HashSet<string> usedIds)
        {
            if (choice == null || !slot.Matches(choice))
                return false;
            if (usedIds == null || string.IsNullOrEmpty(choice.Id))
                return true;
            return !usedIds.Contains(choice.Id);
        }

        static void Remember(HashSet<string> usedIds, ChoiceData choice)
        {
            if (usedIds == null || choice == null || string.IsNullOrEmpty(choice.Id))
                return;
            usedIds.Add(choice.Id);
        }

        static void Shuffle(ChoiceData[] items, System.Random rng)
        {
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                ChoiceData swap = items[i];
                items[i] = items[j];
                items[j] = swap;
            }
        }
    }
}
