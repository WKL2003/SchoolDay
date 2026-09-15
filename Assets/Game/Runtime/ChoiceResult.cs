namespace SchoolDay
{
    public sealed class ChoiceResult
    {
        public ChoiceData Choice;
        public BeatData Beat;
        public MoneyStats StatsBefore;
        public MoneyStats StatsAfter;
        public ExpenseEntry Expense;
        public bool WentBroke;
        public bool Declined;
        public bool Accepted;
        public bool DayEnded;
        public string Weekday;
        public AppliedPromo Promo;
        public float PaidCost;
    }
}
