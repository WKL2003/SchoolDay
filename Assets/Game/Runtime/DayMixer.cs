using System;
using System.Collections.Generic;
using UnityEngine;

namespace SchoolDay
{
    public static class DayMixer
    {
        const string TitleName = "School Day";

        public static DayMix Mix(WeekBoard board, SchoolWeekday weekday, DateTime calendarDay, float leftoverPocket)
        {
            return Mix(board, weekday, calendarDay, leftoverPocket, 0);
        }

        public static DayMix Mix(WeekBoard board, SchoolWeekday weekday, DateTime calendarDay, float leftoverPocket, int playNonce)
        {
            return Mix(board, weekday, calendarDay, leftoverPocket, null, playNonce);
        }

        public static DayMix Mix(
            WeekBoard board,
            SchoolWeekday weekday,
            DateTime calendarDay,
            float leftoverPocket,
            MoneyStats leftoverStats,
            int playNonce)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (board.Day == null)
                throw new InvalidOperationException("WeekBoard needs a DayConfig.");
            if (board.Day.Beats == null || board.Day.Beats.Length == 0)
                throw new InvalidOperationException("DayConfig needs a beat list.");

            int seed = WeekCalendar.MixSeed(board.WeekSeed, weekday, calendarDay.Date, board.ReshuffleNonce, playNonce);
            var rng = new System.Random(seed);
            var usedIds = new HashSet<string>();
            MoneyStats opening = WeekEconomy.Morning(board.Day, leftoverPocket, leftoverStats);
            WakeTime wake = WakeTime.Pick(rng, board.Day);

            PromoData[] activePromos = PromoRules.PickActive(board.Promos, weekday, rng);
            PromoBias bias = PromoRules.BiasFor(board.Promos, activePromos);

            var mix = new DayMix
            {
                Weekday = weekday,
                Seed = seed,
                Opening = opening,
                TitleHeading = Format(board.TitleHeadingFormat, TitleName, opening.Pocket),
                TitleBody = Format(board.TitleBodyFormat, TitleName, opening.Pocket),
                TitleSpeaker = wake.SpeakerLabel,
                Wake = wake,
                ActivePromos = activePromos
            };

            BeatData[] beats = board.Day.Beats;
            for (int i = 0; i < beats.Length; i++)
            {
                BeatData beat = beats[i];
                if (beat == null)
                    continue;

                ChoicePool pool = PoolFor(board, beat);
                ChoiceData[] choices;
                if (pool != null && beat.Phase == BeatPhase.Shock)
                    choices = new[] { pool.PickOne(rng, usedIds, bias) };
                else if (pool != null && beat.Phase == BeatPhase.Trade)
                    choices = pool.PickThree(rng, usedIds, bias);
                else
                    choices = beat.Choices;

                AppliedPromo[] overlays = beat.Phase == BeatPhase.Title
                    ? null
                    : PromoRules.Apply(choices, activePromos, weekday, board.Promos);
                mix.Bind(beat, choices, BackdropFor(beat, choices), overlays);
            }

            return mix;
        }

        public static WakeTime WakeFor(WeekBoard board, SchoolWeekday weekday, DateTime calendarDay, int playNonce)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            int seed = WeekCalendar.MixSeed(board.WeekSeed, weekday, calendarDay.Date, board.ReshuffleNonce, playNonce);
            var rng = new System.Random(seed);
            return WakeTime.Pick(rng, board.Day);
        }

        static ChoicePool PoolFor(WeekBoard board, BeatData beat)
        {
            if (SameBeat(board.Breakfast.Beat, beat))
                return board.Breakfast.Pool;
            if (SameBeat(board.Transit.Beat, beat))
                return board.Transit.Pool;
            if (SameBeat(board.Fomo.Beat, beat))
                return board.Fomo.Pool;
            if (SameBeat(board.Shock.Beat, beat))
                return board.Shock.Pool;
            return null;
        }

        static bool SameBeat(BeatData bound, BeatData beat)
        {
            return bound != null && beat != null && bound == beat;
        }

        static Sprite BackdropFor(BeatData beat, ChoiceData[] choices)
        {
            if (choices != null)
            {
                for (int i = 0; i < choices.Length; i++)
                {
                    if (choices[i] != null && choices[i].Backdrop != null)
                        return choices[i].Backdrop;
                }
            }

            return beat != null ? beat.Background : null;
        }

        static string Format(string template, string titleName, float pocket)
        {
            if (string.IsNullOrEmpty(template))
                return PocketFormat.Cash(pocket) + " in pocket.";

            return string.Format(template, titleName, PocketFormat.Cash(pocket));
        }
    }
}
