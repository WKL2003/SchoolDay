using System.Collections.Generic;
using UnityEngine;

namespace SchoolDay
{
    public sealed class MixOffer
    {
        public MixOffer(ChoiceData[] choices, Sprite background)
            : this(choices, background, null)
        {
        }

        public MixOffer(ChoiceData[] choices, Sprite background, AppliedPromo[] promos)
        {
            Choices = choices;
            Background = background;
            Promos = promos;
        }

        public ChoiceData[] Choices { get; }
        public Sprite Background { get; }
        public AppliedPromo[] Promos { get; }

        public AppliedPromo PromoFor(ChoiceData choice)
        {
            if (Choices == null || Promos == null || choice == null)
                return AppliedPromo.None;

            for (int i = 0; i < Choices.Length; i++)
            {
                if (Choices[i] == choice)
                    return i < Promos.Length ? Promos[i] : AppliedPromo.None;
            }

            return AppliedPromo.None;
        }
    }

    public sealed class DayMix
    {
        readonly Dictionary<BeatData, MixOffer> offers = new Dictionary<BeatData, MixOffer>();

        public SchoolWeekday Weekday { get; set; }
        public int Seed { get; set; }
        public MoneyStats Opening { get; set; }
        public string TitleHeading { get; set; }
        public string TitleBody { get; set; }
        public string TitleSpeaker { get; set; }
        public WakeTime Wake { get; set; }
        public PromoData[] ActivePromos { get; set; }

        public string WeekdayName => Weekday.ToString();

        public void Bind(BeatData beat, ChoiceData[] choices, Sprite background)
        {
            Bind(beat, choices, background, null);
        }

        public void Bind(BeatData beat, ChoiceData[] choices, Sprite background, AppliedPromo[] promos)
        {
            if (beat == null)
                return;

            offers[beat] = new MixOffer(choices, background, promos);
        }

        public bool TryGet(BeatData beat, out MixOffer offer)
        {
            if (beat == null)
            {
                offer = null;
                return false;
            }

            return offers.TryGetValue(beat, out offer);
        }
    }
}
