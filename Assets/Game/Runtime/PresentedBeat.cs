using System.Collections.Generic;
using UnityEngine;

namespace SchoolDay
{
    public sealed class PresentedBeat
    {
        public PresentedBeat(BeatData beat, IReadOnlyList<ChoiceData> choices)
            : this(beat, choices, null, null, null, null, null)
        {
        }

        public PresentedBeat(
            BeatData beat,
            IReadOnlyList<ChoiceData> choices,
            string title,
            string bodyLine,
            string speakerName,
            Sprite background)
            : this(beat, choices, title, bodyLine, speakerName, background, null)
        {
        }

        public PresentedBeat(
            BeatData beat,
            IReadOnlyList<ChoiceData> choices,
            string title,
            string bodyLine,
            string speakerName,
            Sprite background,
            IReadOnlyList<AppliedPromo> promos)
            : this(beat, choices, title, bodyLine, speakerName, background, promos, null)
        {
        }

        public PresentedBeat(
            BeatData beat,
            IReadOnlyList<ChoiceData> choices,
            string title,
            string bodyLine,
            string speakerName,
            Sprite background,
            IReadOnlyList<AppliedPromo> promos,
            string clockDigits)
        {
            Beat = beat;
            Choices = choices;
            Title = title;
            BodyLine = bodyLine;
            SpeakerName = speakerName;
            Background = background;
            Promos = promos;
            ClockDigits = clockDigits;
        }

        public BeatData Beat { get; }
        public IReadOnlyList<ChoiceData> Choices { get; }
        public string Title { get; }
        public string BodyLine { get; }
        public string SpeakerName { get; }
        public Sprite Background { get; }
        public IReadOnlyList<AppliedPromo> Promos { get; }
        public string ClockDigits { get; }

        public AppliedPromo PromoFor(ChoiceData choice)
        {
            if (Choices == null || Promos == null || choice == null)
                return AppliedPromo.None;

            for (int i = 0; i < Choices.Count; i++)
            {
                if (Choices[i] == choice)
                    return i < Promos.Count ? Promos[i] : AppliedPromo.None;
            }

            return AppliedPromo.None;
        }
    }
}
