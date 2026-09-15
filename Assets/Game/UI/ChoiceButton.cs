using System;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class ChoiceButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] Image frame;
        [SerializeField] Text label;
        [SerializeField] Text price;
        [SerializeField] Text prompt;
        [SerializeField] Color idleFrame = new Color(0.17f, 0.20f, 0.24f, 0.94f);
        [SerializeField] Color selectedFrame = new Color(0.32f, 0.30f, 0.24f, 1f);
        [SerializeField] Color priceOk = new Color(0.78f, 0.68f, 0.42f, 1f);
        [SerializeField] Color priceBad = new Color(0.72f, 0.42f, 0.38f, 1f);

        ChoiceData bound;
        Action<ChoiceData> clicked;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public ChoiceData Bound => bound;

        public void Bind(ChoiceData choice, MoneyStats stats, ArtSet art, Action<ChoiceData> onClicked)
        {
            Bind(choice, stats, art, onClicked, true);
        }

        public void Bind(ChoiceData choice, MoneyStats stats, ArtSet art, Action<ChoiceData> onClicked, bool showPrice)
        {
            Bind(choice, stats, art, onClicked, showPrice, AppliedPromo.None);
        }

        public void Bind(
            ChoiceData choice,
            MoneyStats stats,
            ArtSet art,
            Action<ChoiceData> onClicked,
            bool showPrice,
            AppliedPromo promo)
        {
            bound = choice;
            clicked = onClicked;
            gameObject.SetActive(true);

            if (label != null)
                label.text = choice.DisplayName;

            float cost = PromoRules.PaidCost(choice, promo);
            bool canAfford = stats.CanAfford(cost);
            if (button != null)
                button.interactable = true;

            bool priced = showPrice && cost > 0.001f;
            string money = priced ? PromoRules.PriceLabel(PocketFormat.Cash(cost), promo) : string.Empty;
            Color priceColor = canAfford
                ? (promo.Active
                    ? (art != null ? art.Energy : priceOk)
                    : (art != null ? art.PriceOk : priceOk))
                : (art != null ? art.PriceBad : priceBad);

            if (prompt != null)
            {
                prompt.text = money;
                prompt.color = priceColor;
            }

            if (price != null)
            {
                price.text = priced ? PocketFormat.PriceDelta(cost) : string.Empty;
                price.color = priceColor;
            }

            if (icon != null)
            {
                icon.enabled = choice.Icon != null;
                icon.sprite = choice.Icon;
            }
        }

        public void SetSelected(bool selected)
        {
            if (frame == null)
                return;
            frame.color = selected ? selectedFrame : idleFrame;
        }

        void HandleClick()
        {
            if (bound != null)
                clicked?.Invoke(bound);
        }
    }
}
