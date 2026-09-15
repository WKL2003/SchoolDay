namespace SchoolDay
{
    public sealed class ExpenseEntry
    {
        public ExpenseEntry(string label, float amount, string beatId, float time)
        {
            Label = label;
            Amount = amount;
            BeatId = beatId;
            Time = time;
        }

        public string Label { get; }
        public float Amount { get; }
        public string BeatId { get; }
        public float Time { get; }
    }
}
