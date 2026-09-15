namespace SchoolDay
{
    public readonly struct AppliedPromo
    {
        public static readonly AppliedPromo None = default;

        public AppliedPromo(
            PromoData promo,
            float listCost,
            float paidCost,
            string promptLine,
            string badge,
            string expenseFormat)
        {
            Promo = promo;
            ListCost = listCost;
            PaidCost = paidCost;
            PromptLine = promptLine ?? "";
            Badge = badge ?? "";
            ExpenseFormat = expenseFormat ?? "";
        }

        public PromoData Promo { get; }
        public float ListCost { get; }
        public float PaidCost { get; }
        public string PromptLine { get; }
        public string Badge { get; }
        public string ExpenseFormat { get; }
        public bool Active => Promo != null && PaidCost + 0.001f < ListCost;
        public string PromoId => Promo != null ? Promo.Id ?? "" : "";
        public string PromoName => Promo != null ? Promo.DisplayName ?? "" : "";
    }
}
