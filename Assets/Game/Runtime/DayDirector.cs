using System;
using System.Collections.Generic;
using UnityEngine;

namespace SchoolDay
{
    public sealed class DayDirector : MonoBehaviour
    {
        enum PlayScreen
        {
            Select,
            Playing,
            Ended
        }

        struct CommittedChoice
        {
            public string Id;
            public bool Declined;
            public float Time;
        }

        [SerializeField] DayConfig dayConfig;
        [SerializeField] WeekBoard weekBoard;
        [SerializeField] HudView hud;
        [SerializeField] ChoicePanel choices;
        [SerializeField] ExpenseLogView expenseLog;
        [SerializeField] EndCardView endCard;
        [SerializeField] AvatarView avatar;
        [SerializeField] BeatStage stage;
        [SerializeField] CharacterSelectView characterSelect;
        [SerializeField] AchievementView achievements;
        [SerializeField] AchievementCatalog achievementCatalog;

        readonly List<CommittedChoice> committed = new List<CommittedChoice>();

        DaySession session;
        CharacterLook look;
        ChoiceData pending;
        float dayStartedAt;
        bool dayReported;
        DayCarry leftover;
        DayCarry mixCarry;
        SchoolWeekday weekCursor;
        SchoolWeekday mixWeekday;
        DateTime mixDay;
        bool cursorSet;
        bool carryIntoNextDay;
        int playNonce;
        bool reusePlayNonce;
        string lastAchievementId = "";
        string activeSessionId = "";
        bool suppressStartTelemetry;
        float savedElapsed;
        PlayScreen screen;
        bool ready;

        public DaySession Session => session;
        public string LastAchievementId => lastAchievementId;
        public string ActiveSessionId => activeSessionId;
        public bool SuppressStartTelemetry => suppressStartTelemetry;
        public float SavedElapsed => savedElapsed;

        public event Action<DaySession, CharacterLook> DayStarted;
        public event Action<DaySession, CharacterLook> LookChanged;
        public event Action<ChoiceResult> ChoiceCommitted;
        public event Action<DaySession> DayEnded;

        void Awake()
        {
            if (choices != null)
                choices.Wire(OnChoicePicked, OnConfirm, OnSkip);
            if (endCard != null)
                endCard.Wire(NextMorning);
            if (characterSelect != null)
                characterSelect.Wire(OnCharacterConfirmed);
            if (avatar != null)
                avatar.Wire(CycleLook);
        }

        void Start()
        {
            ready = true;
            if (!TryResume())
                ShowCharacterSelect();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
                Persist();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus)
                Persist();
        }

        void OnApplicationQuit()
        {
            Persist();
        }

        public void NextMorning()
        {
            leftover = DayCarry.From(session);
            carryIntoNextDay = leftover != null;
            reusePlayNonce = false;
            suppressStartTelemetry = false;
            savedElapsed = 0f;
            BeginDay();
        }

        void ShowCharacterSelect()
        {
            screen = PlayScreen.Select;
            pending = null;
            if (characterSelect == null)
            {
                BeginDay();
                return;
            }

            if (endCard != null)
                endCard.Hide();
            if (achievements != null)
                achievements.SetTitleChrome(false);
            if (avatar != null)
            {
                avatar.ShowTitlePlace();
                avatar.SetSwapVisible(false);
            }
            characterSelect.Show();
            HoldWakeForSelect();
            Persist();
        }

        void OnCharacterConfirmed(CharacterLook selected)
        {
            ApplyLook(selected);
            BeginDay();
        }

        void CycleLook()
        {
            if (characterSelect == null)
                return;

            CharacterLook current = look ?? characterSelect.Current;
            CharacterLook next = characterSelect.NextAfter(current);
            if (next == null || next == current)
                return;

            ApplyLook(next);
            Persist();
        }

        void ApplyLook(CharacterLook selected)
        {
            if (selected == null)
                return;

            look = selected;
            if (avatar != null)
                avatar.UseLook(selected);
            if (hud != null)
                hud.UseLook(selected);
            if (characterSelect != null)
                characterSelect.Remember(selected);
            LookChanged?.Invoke(session, selected);
        }

        bool TryResume()
        {
            PlaySaveData data = PlaySave.Load();
            if (data == null)
                return false;

            RestoreLook(data.look_id);
            weekCursor = data.cursor_set ? (SchoolWeekday)data.week_cursor : weekCursor;
            cursorSet = data.cursor_set;
            playNonce = data.play_nonce;
            leftover = PlaySave.MixCarry(data);

            if (data.screen == PlaySave.Playing)
                return ResumePlaying(data);

            if (data.screen == PlaySave.Ended)
                return ResumeEnded(data);

            if (data.screen == PlaySave.Select)
            {
                reusePlayNonce = data.play_nonce != 0;
                ShowCharacterSelect();
                return true;
            }

            return false;
        }

        bool ResumePlaying(PlaySaveData data)
        {
            leftover = PlaySave.MixCarry(data);
            carryIntoNextDay = leftover != null;
            reusePlayNonce = true;
            playNonce = data.play_nonce;
            activeSessionId = data.session_id ?? "";
            suppressStartTelemetry = !string.IsNullOrEmpty(activeSessionId);
            savedElapsed = Mathf.Max(0f, data.elapsed);
            if (!BeginDay(data))
            {
                PlaySave.Clear();
                suppressStartTelemetry = false;
                savedElapsed = 0f;
                activeSessionId = "";
                ShowCharacterSelect();
            }

            return true;
        }

        bool ResumeEnded(PlaySaveData data)
        {
            leftover = PlaySave.MixCarry(data);
            carryIntoNextDay = leftover != null;
            reusePlayNonce = true;
            playNonce = data.play_nonce;
            activeSessionId = data.session_id ?? "";
            suppressStartTelemetry = true;
            savedElapsed = Mathf.Max(0f, data.elapsed);
            if (!BeginDay(data) || session == null || !session.IsDayOver)
            {
                PlaySave.Clear();
                suppressStartTelemetry = false;
                savedElapsed = 0f;
                activeSessionId = "";
                ShowCharacterSelect();
                return true;
            }

            screen = PlayScreen.Ended;
            dayReported = true;
            weekCursor = data.cursor_set ? (SchoolWeekday)data.week_cursor : weekCursor;
            cursorSet = data.cursor_set;
            ShowEnd();
            return true;
        }

        public void BeginDay()
        {
            BeginDay(null);
        }

        bool BeginDay(PlaySaveData resume)
        {
            DayConfig config = dayConfig;
            if (weekBoard != null && weekBoard.Day != null)
                config = weekBoard.Day;
            if (config == null)
                throw new InvalidOperationException("DayDirector needs a DayConfig.");

            bool resuming = resume != null;
            DateTime calendarDay = DateTime.Now.Date;
            SchoolWeekday weekday = WeekCalendar.Resolve(weekBoard, DateTime.Now);
            DayCarry carried = carryIntoNextDay ? leftover : null;
            int nonce = reusePlayNonce ? playNonce : NewPlayNonce();

            if (resuming)
            {
                calendarDay = PlaySave.CalendarDay(resume);
                SchoolWeekday savedDay;
                if (PlaySave.TryWeekday(resume, out savedDay))
                    weekday = savedDay;
                carried = PlaySave.MixCarry(resume);
                nonce = resume.play_nonce != 0 ? resume.play_nonce : nonce;
                if (string.IsNullOrEmpty(activeSessionId))
                    activeSessionId = resume.session_id ?? "";
            }
            else
            {
                if (weekBoard != null && weekBoard.PlayFullWeek && cursorSet)
                    weekday = weekCursor;
                activeSessionId = Telemetry.NewSessionId();
                suppressStartTelemetry = false;
                savedElapsed = 0f;
            }

            reusePlayNonce = false;
            playNonce = nonce;
            mixCarry = carried;
            mixWeekday = weekday;
            mixDay = calendarDay;

            DayMix mix = null;
            if (weekBoard != null)
            {
                float leftoverPocket = carried != null && carried.Stats != null ? carried.Stats.Pocket : 0f;
                mix = DayMixer.Mix(
                    weekBoard,
                    weekday,
                    calendarDay,
                    leftoverPocket,
                    carried != null ? carried.Stats : null,
                    nonce);
            }

            leftover = null;
            carryIntoNextDay = false;
            lastAchievementId = "";
            pending = null;
            dayReported = false;
            committed.Clear();

            session = new DaySession();
            session.StartDay(config, mix, carried);

            if (resuming && !Replay(resume))
                return false;

            screen = session.IsDayOver ? PlayScreen.Ended : PlayScreen.Playing;
            dayStartedAt = Time.unscaledTime - savedElapsed;

            if (characterSelect != null)
                characterSelect.Hide();

            if (expenseLog != null && !resuming)
                expenseLog.Clear();
            if (endCard != null && !session.IsDayOver)
                endCard.Hide();
            if (choices != null)
                choices.gameObject.SetActive(!session.IsDayOver);
            if (stage != null)
                stage.gameObject.SetActive(!session.IsDayOver);

            PaintStats(session.Stats, false);
            if (look != null)
                ApplyLook(look);
            if (!session.IsDayOver)
                DayStarted?.Invoke(session, look);
            if (!session.IsDayOver)
                ShowCurrentBeat();
            Persist();
            return true;
        }

        bool Replay(PlaySaveData data)
        {
            if (expenseLog != null)
                expenseLog.Clear();

            if (data == null || data.choice_ids == null)
                return true;

            for (int i = 0; i < data.choice_ids.Length; i++)
            {
                bool skip = data.declined != null && i < data.declined.Length && data.declined[i];
                float time = data.choice_times != null && i < data.choice_times.Length
                    ? data.choice_times[i]
                    : 0f;
                ChoiceResult result;
                if (!session.TryReplay(data.choice_ids[i], time, skip, out result))
                    return false;

                RememberChoice(result, time);
                if (result.Expense != null && expenseLog != null)
                    expenseLog.Append(result.Expense);
            }

            return true;
        }

        void OnChoicePicked(ChoiceData choice)
        {
            pending = choice;
            if (choices != null)
                choices.ShowConfirm(choice, session.Stats);
        }

        void OnConfirm()
        {
            CommitPending(false);
        }

        void OnSkip()
        {
            CommitPending(true);
        }

        void CommitPending(bool skip)
        {
            if (pending == null || session == null || session.IsDayOver)
                return;

            float time = Time.unscaledTime - dayStartedAt;
            ChoiceResult result = session.TryChoose(pending, time, skip);
            pending = null;
            if (!result.Accepted)
                return;

            RememberChoice(result, time);
            ChoiceCommitted?.Invoke(result);

            if (result.Expense != null && expenseLog != null)
                expenseLog.Append(result.Expense);

            PaintStats(result.StatsAfter, true);

            if (session.IsDayOver)
                ShowEnd();
            else
                ShowCurrentBeat();
            Persist();
        }

        void RememberChoice(ChoiceResult result, float time)
        {
            if (result == null || result.Choice == null)
                return;

            committed.Add(new CommittedChoice
            {
                Id = result.Choice.Id ?? "",
                Declined = result.Declined,
                Time = time
            });
        }

        void ShowCurrentBeat()
        {
            PresentedBeat presented = session.PresentBeat();
            if (presented == null)
            {
                ShowEnd();
                return;
            }

            if (stage != null)
                stage.Bind(presented);
            if (choices != null)
            {
                choices.gameObject.SetActive(true);
                bool titleBeat = presented.Beat != null && presented.Beat.Phase == BeatPhase.Title;
                BeatPhase phase = presented.Beat != null ? presented.Beat.Phase : BeatPhase.Trade;
                if (titleBeat)
                    choices.Bind(null, session.Stats, false, phase);
                else
                    choices.Bind(presented.Choices, session.Stats, true, phase, presented.Promos);

                bool autoConfirm = presented.Choices != null
                    && presented.Choices.Count == 1
                    && presented.Choices[0] != null
                    && presented.Beat != null
                    && (presented.Beat.Phase == BeatPhase.Shock || presented.Beat.Phase == BeatPhase.Title);
                if (autoConfirm)
                {
                    pending = presented.Choices[0];
                    choices.ShowConfirm(pending, session.Stats);
                }
            }

            bool title = presented.Beat != null && presented.Beat.Phase == BeatPhase.Title;
            if (avatar != null)
            {
                if (title)
                    avatar.ShowTitlePlace();
                else
                    avatar.ShowPlayPlace();
                avatar.SetSwapVisible(title);
            }
            if (achievements != null)
                achievements.SetTitleChrome(title);
        }

        void ShowEnd()
        {
            screen = PlayScreen.Ended;
            if (choices != null)
                choices.Hide();
            if (stage != null)
                stage.HideCopy();
            if (avatar != null)
                avatar.SetSwapVisible(false);
            if (achievements != null)
                achievements.SetTitleChrome(false);

            lastAchievementId = AchievementSave.CloseDay(achievementCatalog, session);
            if (achievements != null)
                achievements.Refresh();

            if (endCard != null)
                endCard.Show(session);

            if (dayReported)
            {
                Persist();
                return;
            }

            dayReported = true;
            AdvanceWeek();
            DayEnded?.Invoke(session);
            Persist();
        }

        void PaintStats(MoneyStats stats, bool animate)
        {
            if (hud != null)
                hud.Bind(stats, session != null ? session.Reputation : default, animate);
            if (avatar != null)
                avatar.Bind(stats, session != null ? session.Reputation.Percent : (stats != null ? stats.Face : 0f));
        }

        void HoldWakeForSelect()
        {
            if (!reusePlayNonce)
                playNonce = NewPlayNonce();
            reusePlayNonce = true;
            if (characterSelect == null || weekBoard == null)
                return;

            SchoolWeekday weekday = WeekCalendar.Resolve(weekBoard, DateTime.Now);
            if (weekBoard.PlayFullWeek && cursorSet)
                weekday = weekCursor;
            WakeTime wake = DayMixer.WakeFor(weekBoard, weekday, DateTime.Now.Date, playNonce);
            characterSelect.BindDeskClock(wake.ClockDigits);
        }

        static int NewPlayNonce()
        {
            return Environment.TickCount ^ Guid.NewGuid().GetHashCode();
        }

        void AdvanceWeek()
        {
            if (weekBoard == null || !weekBoard.PlayFullWeek || session == null)
            {
                cursorSet = false;
                return;
            }

            if (session.Weekday == SchoolWeekday.Friday)
            {
                if (weekBoard.AfterFriday == FridayWrap.Stop)
                    cursorSet = false;
                else
                {
                    weekCursor = SchoolWeekday.Monday;
                    cursorSet = true;
                }

                return;
            }

            weekCursor = WeekCalendar.Next(session.Weekday);
            cursorSet = true;
        }

        void RestoreLook(string lookId)
        {
            CharacterLook found = characterSelect != null ? characterSelect.FindLook(lookId) : null;
            if (found != null)
                ApplyLook(found);
        }

        void Persist()
        {
            if (!ready)
                return;

            PlaySaveData data = new PlaySaveData
            {
                screen = ScreenName(screen),
                session_id = activeSessionId ?? "",
                look_id = look != null ? look.DisplayName : LookName(),
                weekday = session != null ? session.WeekdayName : (mixWeekday != 0 ? mixWeekday.ToString() : ""),
                calendar_day = PlaySave.DateStamp(mixDay != default ? mixDay : DateTime.Now.Date),
                play_nonce = playNonce,
                week_cursor = (int)weekCursor,
                cursor_set = cursorSet,
                elapsed = session != null ? Mathf.Max(0f, Time.unscaledTime - dayStartedAt) : 0f
            };

            WriteMixCarry(data, mixCarry);
            WriteStats(data, session != null ? session.Stats : null);
            WriteChoices(data);
            PlaySave.Write(data);
        }

        void WriteMixCarry(PlaySaveData data, DayCarry carry)
        {
            if (data == null)
                return;

            data.has_mix_carry = carry != null && carry.Stats != null;
            if (!data.has_mix_carry)
                return;

            data.mix_pocket = carry.Stats.Pocket;
            data.mix_fuel = carry.Stats.Fuel;
            data.mix_face = carry.Stats.Face;
            data.mix_buffer = carry.Stats.Buffer;
            data.mix_leak = (int)carry.Leak;
            data.mix_shock_reached = carry.ShockReached;
            data.mix_shock_survived = carry.ShockSurvived;
        }

        static void WriteStats(PlaySaveData data, MoneyStats stats)
        {
            if (data == null || stats == null)
                return;

            data.pocket = stats.Pocket;
            data.fuel = stats.Fuel;
            data.face = stats.Face;
            data.buffer = stats.Buffer;
        }

        void WriteChoices(PlaySaveData data)
        {
            if (data == null)
                return;

            data.choice_ids = new string[committed.Count];
            data.declined = new bool[committed.Count];
            data.choice_times = new float[committed.Count];
            for (int i = 0; i < committed.Count; i++)
            {
                data.choice_ids[i] = committed[i].Id ?? "";
                data.declined[i] = committed[i].Declined;
                data.choice_times[i] = committed[i].Time;
            }
        }

        string LookName()
        {
            CharacterLook current = look;
            if (current == null && characterSelect != null)
                current = characterSelect.Current;
            return current != null ? current.DisplayName : "";
        }

        static string ScreenName(PlayScreen value)
        {
            switch (value)
            {
                case PlayScreen.Select:
                    return PlaySave.Select;
                case PlayScreen.Playing:
                    return PlaySave.Playing;
                case PlayScreen.Ended:
                    return PlaySave.Ended;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}
