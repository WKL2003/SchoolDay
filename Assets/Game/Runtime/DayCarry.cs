namespace SchoolDay
{
    public sealed class DayCarry
    {
        public MoneyStats Stats;
        public LeakTag Leak;
        public bool ShockReached;
        public bool ShockSurvived;

        public static DayCarry From(DaySession session)
        {
            if (session == null || session.Stats == null)
                return null;

            return new DayCarry
            {
                Stats = session.Stats.Clone(),
                Leak = session.NamedLeak != LeakTag.None ? session.NamedLeak : session.CurrentLeak,
                ShockReached = session.ShockChoice != null || session.IsDayOver,
                ShockSurvived = session.ShockSurvived
            };
        }
    }
}
