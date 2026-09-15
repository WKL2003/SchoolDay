using System;
using System.Globalization;

namespace SchoolDay
{
    public static class Telemetry
    {
        public const string SessionStart = "session_start";
        public const string Choice = "choice";
        public const string SessionEnd = "session_end";

        public static string NewSessionId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string NewPlayerId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static TelemetryEvent Started(string sessionId, DaySession session, string lookId)
        {
            return Started(sessionId, session, lookId, "");
        }

        public static TelemetryEvent Started(string sessionId, DaySession session, string lookId, string playerId)
        {
            TelemetryEvent ev = Blank(sessionId, SessionStart, playerId);
            ev.look_id = lookId ?? "";
            ev.weekday = WeekdayName(session);
            WriteStats(ev, session != null ? session.Stats : null);
            WriteReputation(ev, session);
            return ev;
        }

        public static TelemetryEvent FromChoice(string sessionId, ChoiceResult result, DayConfig config)
        {
            return FromChoice(sessionId, result, config, "");
        }

        public static TelemetryEvent FromChoice(string sessionId, ChoiceResult result, DayConfig config, string playerId)
        {
            TelemetryEvent ev = Blank(sessionId, Choice, playerId);
            if (result == null)
                return ev;

            if (result.Beat != null)
            {
                ev.beat_id = result.Beat.Id ?? "";
                ev.beat_name = result.Beat.Title ?? "";
            }

            if (result.Choice != null)
            {
                ev.choice_id = result.Choice.Id ?? "";
                ev.choice_name = result.Declined ? "Skipped" : (result.Choice.DisplayName ?? "");
                ev.cost = result.Declined ? 0f : result.PaidCost;
                ev.named_leak = LeakName(config, result.Choice.LeakTag);
                ev.promo_id = result.Promo.Active ? result.Promo.PromoId : "";
            }

            WriteStats(ev, result.StatsAfter);
            ev.went_broke = result.WentBroke;
            ev.declined = result.Declined;
            ev.day_ended = result.DayEnded;
            ev.weekday = result.Weekday ?? "";
            return ev;
        }

        public static void WriteReputation(TelemetryEvent ev, DaySession session)
        {
            if (ev == null)
                return;

            ReputationScore score = ReputationRules.FromSession(session);
            ev.punctual = score.Punctual;
            ev.friendly = score.Friendly;
            ev.adaptive = score.Adaptive;
            ev.generous = score.Generous;
            ev.reputation = score.Percent;
        }

        public static TelemetryEvent Ended(string sessionId, DaySession session, float duration)
        {
            return Ended(sessionId, session, duration, "", "");
        }

        public static TelemetryEvent Ended(string sessionId, DaySession session, float duration, string achievementId)
        {
            return Ended(sessionId, session, duration, achievementId, "");
        }

        public static TelemetryEvent Ended(string sessionId, DaySession session, float duration, string achievementId, string playerId)
        {
            TelemetryEvent ev = Blank(sessionId, SessionEnd, playerId);
            if (session == null)
                return ev;

            WriteStats(ev, session.Stats);
            ev.weekday = WeekdayName(session);
            ev.shock_survived = session.ShockSurvived;
            ev.named_leak = LeakName(session.Config, session.NamedLeak);
            ev.day_ended = true;
            ev.won = session.Won;
            ev.buffer_kid = session.IsBufferKid;
            ev.duration = duration;
            ev.achievement_id = achievementId ?? "";
            WriteReputation(ev, session);
            return ev;
        }

        static TelemetryEvent Blank(string sessionId, string kind)
        {
            return Blank(sessionId, kind, "");
        }

        static TelemetryEvent Blank(string sessionId, string kind, string playerId)
        {
            return new TelemetryEvent
            {
                session_id = sessionId ?? "",
                player_id = playerId ?? "",
                kind = kind,
                ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                look_id = "",
                weekday = "",
                beat_id = "",
                beat_name = "",
                choice_id = "",
                choice_name = "",
                named_leak = "",
                achievement_id = "",
                promo_id = ""
            };
        }

        static void WriteStats(TelemetryEvent ev, MoneyStats stats)
        {
            if (stats == null)
                return;

            ev.pocket = stats.Pocket;
            ev.fuel = stats.Fuel;
            ev.face = stats.Face;
            ev.buffer = stats.Buffer;
        }

        static string LeakName(DayConfig config, LeakTag tag)
        {
            if (config == null || tag == LeakTag.None)
                return "";

            return config.LeakName(tag);
        }

        static string WeekdayName(DaySession session)
        {
            return session != null ? session.WeekdayName : "";
        }
    }
}
