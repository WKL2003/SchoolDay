using System;
using System.Collections.Generic;

namespace SchoolDay
{
    public sealed class DaySession
    {
        readonly List<ExpenseEntry> expenses = new List<ExpenseEntry>();
        readonly List<LeakTag> leaks = new List<LeakTag>();
        readonly List<ChoiceData> shockOffer = new List<ChoiceData>(1);

        DayConfig config;
        DayMix mix;
        MoneyStats stats;
        int beatIndex;
        ChoiceData lockedShock;
        bool goneBroke;
        bool shockSurvived;
        bool dayOver;
        CoachLine coach;
        LeakTag carriedLeak;
        float reputationFuel;
        bool lockReputationFuel;

        public MoneyStats Stats => stats;
        public DayConfig Config => config;
        public DayMix Mix => mix;
        public SchoolWeekday Weekday => mix != null ? mix.Weekday : default;
        public string WeekdayName => mix != null ? mix.WeekdayName : "";
        public int BeatIndex => beatIndex;
        public IReadOnlyList<ExpenseEntry> Expenses => expenses;
        public bool ShockSurvived => shockSurvived;
        public bool GoneBroke => goneBroke;
        public bool IsDayOver => dayOver;
        public LeakTag NamedLeak { get; private set; }
        public bool FuelMet { get; private set; }
        public bool IsBufferKid { get; private set; }
        public bool Won { get; private set; }
        public ChoiceData ShockChoice { get; private set; }
        public CoachLine Coach => coach;
        public LeakTag CurrentLeak => config != null
            ? LeakRules.NameLeak(leaks, config.LeakPriority)
            : LeakTag.None;
        public LeakTag ReputationLeak => CurrentLeak != LeakTag.None ? CurrentLeak : carriedLeak;
        public bool ReputationShockReached => ShockChoice != null || dayOver;
        public bool ReputationShockPaid => shockSurvived;
        public float ReputationFuel => lockReputationFuel ? reputationFuel : stats.Fuel;
        public ReputationScore Reputation => ReputationRules.FromSession(this);

        public void StartDay(DayConfig dayConfig)
        {
            StartDay(dayConfig, null);
        }

        public void StartDay(DayConfig dayConfig, DayMix dayMix)
        {
            StartDay(dayConfig, dayMix, null);
        }

        public void StartDay(DayConfig dayConfig, DayMix dayMix, DayCarry carry)
        {
            if (dayConfig == null)
                throw new ArgumentNullException(nameof(dayConfig));
            if (dayConfig.Beats == null || dayConfig.Beats.Length == 0)
                throw new InvalidOperationException("DayConfig needs a beat list.");

            config = dayConfig;
            mix = dayMix;
            stats = dayMix != null && dayMix.Opening != null
                ? dayMix.Opening.Clone()
                : dayConfig.CreateOpeningStats();
            dayConfig.Clamp(stats);
            beatIndex = 0;
            lockedShock = null;
            ShockChoice = null;
            goneBroke = false;
            shockSurvived = false;
            dayOver = false;
            NamedLeak = LeakTag.None;
            FuelMet = false;
            IsBufferKid = false;
            Won = false;
            coach = default;
            expenses.Clear();
            leaks.Clear();
            carriedLeak = carry != null ? carry.Leak : LeakTag.None;
            bool hasCarriedFuel = carry != null && carry.Stats != null;
            reputationFuel = hasCarriedFuel ? carry.Stats.Fuel : stats.Fuel;
            lockReputationFuel = hasCarriedFuel;
        }

        public PresentedBeat PresentBeat()
        {
            EnsureStarted();
            if (dayOver)
                return null;

            BeatData beat = config.Beats[beatIndex];
            MixOffer offer = null;
            bool mixed = mix != null && mix.TryGet(beat, out offer) && offer != null && offer.Choices != null;

            if (beat.Phase != BeatPhase.Shock)
            {
                IReadOnlyList<ChoiceData> trade = mixed ? offer.Choices : beat.Choices;
                return Overlay(beat, trade, mixed ? offer : null);
            }

            if (lockedShock == null)
            {
                if (mixed && offer.Choices.Length > 0)
                    lockedShock = offer.Choices[0];
                else
                    lockedShock = PickShock(beat);
            }

            shockOffer.Clear();
            shockOffer.Add(lockedShock);
            return Overlay(beat, shockOffer, mixed ? offer : null);
        }

        public ChoiceResult TryChoose(ChoiceData choice, float timeSeconds)
        {
            return TryChoose(choice, timeSeconds, false);
        }

        public ChoiceData FindOffered(string choiceId)
        {
            if (string.IsNullOrEmpty(choiceId))
                return null;

            PresentedBeat presented = PresentBeat();
            if (presented == null || presented.Choices == null)
                return null;

            for (int i = 0; i < presented.Choices.Count; i++)
            {
                ChoiceData choice = presented.Choices[i];
                if (choice != null && choice.Id == choiceId)
                    return choice;
            }

            return null;
        }

        public bool TryReplay(string choiceId, float timeSeconds, bool skip, out ChoiceResult result)
        {
            ChoiceData choice = FindOffered(choiceId);
            if (choice == null)
            {
                result = Rejected(null);
                return false;
            }

            result = TryChoose(choice, timeSeconds, skip);
            return result.Accepted;
        }

        public ChoiceResult TryChoose(ChoiceData choice, float timeSeconds, bool skip)
        {
            EnsureStarted();
            if (dayOver || choice == null)
                return Rejected(choice);

            PresentedBeat presented = PresentBeat();
            if (presented == null || !Offers(presented.Choices, choice))
                return Rejected(choice);

            if (skip && choice.Overspend != OverspendRule.Decline)
                return Rejected(choice);

            MoneyStats before = stats.Clone();
            AppliedPromo promo = presented.PromoFor(choice);
            float cost = PromoRules.PaidCost(choice, promo);
            bool declined = false;
            bool brokeNow = false;

            if (skip)
            {
                declined = true;
            }
            else
            {
                bool shortNow = !stats.CanAfford(cost);
                stats.Pocket -= cost;
                if (shortNow)
                {
                    goneBroke = true;
                    brokeNow = true;
                }

                ApplyNonPocket(choice, before);
            }

            if (!declined)
            {
                LeakTag leak = choice.LeakTag != LeakTag.None ? choice.LeakTag : choice.Delta.MarksLeak;
                if (leak != LeakTag.None)
                    leaks.Add(leak);
            }

            stats.Buffer = stats.Pocket;
            config.Clamp(stats);

            if (presented.Beat.Phase == BeatPhase.Shock)
            {
                ShockChoice = choice;
                shockSurvived = !declined;
                float faceFromChoice = declined ? 0f : FaceGain(choice, before);
                stats.Face = ReputationRules.AfterShockFace(
                    stats.Face,
                    config,
                    shockSurvived,
                    stats.Pocket,
                    faceFromChoice);
                config.Clamp(stats);
            }

            ExpenseEntry expense = null;
            if (presented.Beat.Phase != BeatPhase.Title)
            {
                float amount = declined ? 0f : cost;
                string label = declined ? "Skipped" : PromoRules.ExpenseLabel(choice, promo);
                expense = new ExpenseEntry(label, amount, presented.Beat.Id, timeSeconds);
                expenses.Add(expense);
            }

            beatIndex++;
            if (beatIndex >= config.Beats.Length)
                CloseDay();

            return new ChoiceResult
            {
                Choice = choice,
                Beat = presented.Beat,
                StatsBefore = before,
                StatsAfter = stats.Clone(),
                Expense = expense,
                WentBroke = brokeNow,
                Declined = declined,
                Accepted = true,
                DayEnded = dayOver,
                Weekday = WeekdayName,
                Promo = promo,
                PaidCost = declined ? 0f : cost
            };
        }

        void CloseDay()
        {
            dayOver = true;
            NamedLeak = LeakRules.NameLeak(leaks, config.LeakPriority);
            FuelMet = stats.Fuel + 0.001f >= config.FuelMetThreshold;
            IsBufferKid = stats.Pocket + 0.001f >= config.BufferWin;
            Won = FuelMet && IsBufferKid;
            coach = config.PickCoachLine(NamedLeak, IsBufferKid, goneBroke, !FuelMet);
        }

        void ApplyNonPocket(ChoiceData choice, MoneyStats before)
        {
            float fuelBefore = stats.Fuel;
            stats.Fuel += choice.Delta.FuelChange;
            stats.Face += FaceGain(choice, before);
            stats.Buffer += choice.Delta.BufferChange;
            ApplyNonPocketExtras(choice, before);
            if (Math.Abs(stats.Fuel - fuelBefore) > 0.001f)
                lockReputationFuel = false;
        }

        static float FaceGain(ChoiceData choice, MoneyStats before)
        {
            float faceDelta = choice.Delta.FaceChange;
            if (faceDelta > 0.001f && before != null && before.Pocket < -0.001f)
                return 0f;
            return faceDelta;
        }

        void ApplyNonPocketExtras(ChoiceData choice, MoneyStats before)
        {
            stats.Fuel += RuleBook.ExtraFuel(choice, before, config);
            stats.Face += RuleBook.DebtReputation(before.Pocket, stats.Pocket, config);
        }

        ChoiceData PickShock(BeatData beat)
        {
            ChoiceData[] pool = beat.Choices;
            if (pool == null || pool.Length == 0)
                throw new InvalidOperationException("Shock beat needs ChoiceData entries.");

            int seed = config.ShockSeed;
            var rng = seed == 0
                ? new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode())
                : new Random(seed);
            return pool[rng.Next(0, pool.Length)];
        }

        static bool Offers(IReadOnlyList<ChoiceData> choices, ChoiceData choice)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i] == choice)
                    return true;
            }

            return false;
        }

        ChoiceResult Rejected(ChoiceData choice)
        {
            return new ChoiceResult
            {
                Choice = choice,
                Beat = !dayOver && config != null ? config.Beats[beatIndex] : null,
                StatsBefore = stats != null ? stats.Clone() : null,
                StatsAfter = stats != null ? stats.Clone() : null,
                Accepted = false,
                Weekday = WeekdayName
            };
        }

        PresentedBeat Overlay(BeatData beat, IReadOnlyList<ChoiceData> choices, MixOffer offer)
        {
            bool title = mix != null && beat != null && beat.Phase == BeatPhase.Title;
            return new PresentedBeat(
                beat,
                choices,
                title ? mix.TitleHeading : null,
                title ? mix.TitleBody : null,
                title ? mix.TitleSpeaker : null,
                offer != null ? offer.Background : null,
                offer != null ? offer.Promos : null,
                title ? mix.Wake.ClockDigits : null);
        }

        void EnsureStarted()
        {
            if (config == null || stats == null)
                throw new InvalidOperationException("Call StartDay before using the session.");
        }
    }
}
