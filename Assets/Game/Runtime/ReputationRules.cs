using UnityEngine;

namespace SchoolDay
{
    public readonly struct ReputationScore
    {
        public ReputationScore(float punctual, float friendly, float adaptive, float generous, float total)
        {
            Punctual = punctual;
            Friendly = friendly;
            Adaptive = adaptive;
            Generous = generous;
            Total = total;
        }

        public float Punctual { get; }
        public float Friendly { get; }
        public float Adaptive { get; }
        public float Generous { get; }
        public float Total { get; }
        public int Percent => Mathf.Clamp(Mathf.RoundToInt(Total), 0, 100);
    }

    public static class ReputationRules
    {
        public static ReputationScore FromSession(DaySession session)
        {
            if (session == null || session.Stats == null)
                return Score(session != null ? session.Config : null, null, LeakTag.None, false, false, false);

            return Score(
                session.Config,
                session.Stats,
                session.ReputationLeak,
                session.GoneBroke,
                session.ReputationShockReached,
                session.ReputationShockPaid);
        }

        public static ReputationScore Score(
            DayConfig config,
            MoneyStats stats,
            LeakTag leak,
            bool wentBroke,
            bool shockReached,
            bool shockPaid)
        {
            float cap = config != null && config.StatCap > 0.001f ? config.StatCap : 1f;
            float friendly = ClampStat(stats != null ? stats.Face : 0f, cap);
            float adaptive = Adaptive(config, stats, leak, wentBroke);
            float generous = Generous(config, cap, shockReached, shockPaid);
            float total = friendly * DebtFactor(config, stats);
            return new ReputationScore(0f, friendly, adaptive, generous, total);
        }

        public static float AfterShockFace(
            float face,
            DayConfig config,
            bool shockPaid,
            float pocketAfter,
            float faceFromChoice)
        {
            if (!shockPaid)
            {
                float hit = config != null ? config.ShockSkipFaceHit : 12f;
                return face - hit;
            }

            if (pocketAfter < -0.001f)
                return face;

            float pay = config != null ? config.ShockPayFaceGain : 6f;
            float extra = Mathf.Max(0f, pay - Mathf.Max(0f, faceFromChoice));
            return face + extra;
        }

        static float Generous(DayConfig config, float cap, bool shockReached, bool shockPaid)
        {
            if (!shockReached)
                return config != null ? config.ReputationGenerousPending : 0f;
            return shockPaid ? cap : 0f;
        }

        static float Adaptive(DayConfig config, MoneyStats stats, LeakTag leak, bool wentBroke)
        {
            if (wentBroke || (stats != null && stats.Pocket < -0.001f))
                return 0f;

            float keepLine = config != null ? config.BufferWin : 0f;
            float cap = config != null && config.StatCap > 0.001f ? config.StatCap : 1f;
            float pocket = stats != null ? stats.Pocket : 0f;
            float keep;
            if (keepLine <= 0.001f)
                keep = pocket > 0f ? cap : 0f;
            else if (pocket + 0.001f >= keepLine)
                keep = cap;
            else if (pocket > 0f)
                keep = cap * pocket / keepLine;
            else
                keep = 0f;

            if (leak == LeakTag.None || config == null)
                return keep;

            return keep * config.ReputationLeakKeepScale;
        }

        static float DebtFactor(DayConfig config, MoneyStats stats)
        {
            float zeroAt = config != null ? config.DebtReputationZeroAt : 0f;
            if (zeroAt <= 0.001f || stats == null)
                return 1f;

            float debt = Mathf.Max(0f, -stats.Pocket);
            return Mathf.Clamp01(1f - debt / zeroAt);
        }

        static float ClampStat(float value, float cap)
        {
            if (cap <= 0.001f)
                return 0f;
            return Mathf.Clamp(100f * value / cap, 0f, 100f);
        }
    }
}
