using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class ChoicePanel : MonoBehaviour
    {
        [SerializeField] ChoiceButton buttonPrefab;
        [SerializeField] Transform buttonRoot;
        [SerializeField] ArtSet art;
        [SerializeField] Button startButton;
        [SerializeField] Text startLabel;
        [SerializeField] Button confirmButton;
        [SerializeField] Text confirmLabel;
        [SerializeField] Button skipButton;
        [SerializeField] Text skipLabel;
        [SerializeField] string payFormat = "Pay {0}";
        [SerializeField] string freeFormat = "Confirm";
        [SerializeField] string skipFormat = "Skip";

        readonly List<ChoiceButton> pool = new List<ChoiceButton>();
        Action<ChoiceData> picked;
        Action confirmed;
        Action skipped;
        ChoiceData pending;
        MoneyStats latestStats;
        BeatPhase latestPhase;
        IReadOnlyList<ChoiceData> latestChoices;
        IReadOnlyList<AppliedPromo> latestPromos;

        void Awake()
        {
            if (buttonPrefab != null)
                buttonPrefab.gameObject.SetActive(false);
            if (startButton != null)
                startButton.onClick.AddListener(HandleConfirm);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);
            if (skipButton != null)
                skipButton.onClick.AddListener(HandleSkip);
        }

        public void Wire(Action<ChoiceData> onPicked, Action onConfirmed)
        {
            Wire(onPicked, onConfirmed, null);
        }

        public void Wire(Action<ChoiceData> onPicked, Action onConfirmed, Action onSkipped)
        {
            picked = onPicked;
            confirmed = onConfirmed;
            skipped = onSkipped;
        }

        public void Bind(IReadOnlyList<ChoiceData> choices, MoneyStats stats)
        {
            Bind(choices, stats, true, BeatPhase.Trade);
        }

        public void Bind(IReadOnlyList<ChoiceData> choices, MoneyStats stats, bool showPrices)
        {
            Bind(choices, stats, showPrices, BeatPhase.Trade);
        }

        public void Bind(IReadOnlyList<ChoiceData> choices, MoneyStats stats, bool showPrices, BeatPhase phase)
        {
            Bind(choices, stats, showPrices, phase, null);
        }

        public void Bind(
            IReadOnlyList<ChoiceData> choices,
            MoneyStats stats,
            bool showPrices,
            BeatPhase phase,
            IReadOnlyList<AppliedPromo> promos)
        {
            latestStats = stats;
            latestPhase = phase;
            latestChoices = choices;
            latestPromos = promos;
            pending = null;
            HideConfirm();

            int needed = choices != null ? choices.Count : 0;
            for (int i = 0; i < needed; i++)
            {
                ChoiceButton slot = GetSlot(i);
                AppliedPromo promo = PromoAt(i);
                slot.Bind(choices[i], stats, art, HandlePicked, showPrices, promo);
                slot.SetSelected(false);
            }

            for (int i = needed; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        public void ShowConfirm(ChoiceData choice, MoneyStats stats)
        {
            pending = choice;
            latestStats = stats;

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].gameObject.activeSelf)
                    pool[i].SetSelected(pool[i].Bound == choice);
            }

            bool title = latestPhase == BeatPhase.Title;
            Button shown = title ? TitleButton() : confirmButton;
            Button hidden = title ? confirmButton : TitleButton();
            if (hidden != null && hidden != shown)
                hidden.gameObject.SetActive(false);

            if (shown == null)
                return;

            shown.gameObject.SetActive(true);
            shown.interactable = true;

            bool showSkip = skipButton != null && !title && choice.Overspend == OverspendRule.Decline;
            if (skipButton != null)
                skipButton.gameObject.SetActive(showSkip);
            if (showSkip && skipLabel != null)
                skipLabel.text = PlainLabel(skipFormat);

            Text label = title ? TitleLabel() : confirmLabel;
            if (label == null)
                return;

            float cost = PromoRules.PaidCost(choice, PromoFor(choice));
            if (title)
                label.text = choice.DisplayName ?? string.Empty;
            else if (cost <= 0.001f)
                label.text = PlainLabel(freeFormat);
            else
                label.text = FormatPay(cost);
        }

        public void Hide()
        {
            pending = null;
            HideConfirm();
            for (int i = 0; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        void HandlePicked(ChoiceData choice)
        {
            if (pending == choice)
                return;

            ShowConfirm(choice, latestStats);
            picked?.Invoke(choice);
        }

        void HandleConfirm()
        {
            confirmed?.Invoke();
        }

        void HandleSkip()
        {
            skipped?.Invoke();
        }

        void HideConfirm()
        {
            if (startButton != null)
                startButton.gameObject.SetActive(false);
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);
            if (skipButton != null)
                skipButton.gameObject.SetActive(false);
        }

        Button TitleButton()
        {
            return startButton != null ? startButton : confirmButton;
        }

        Text TitleLabel()
        {
            return startLabel != null ? startLabel : confirmLabel;
        }

        ChoiceButton GetSlot(int index)
        {
            while (pool.Count <= index)
            {
                ChoiceButton created = Instantiate(buttonPrefab, buttonRoot != null ? buttonRoot : transform);
                created.gameObject.SetActive(true);
                pool.Add(created);
            }

            return pool[index];
        }

        AppliedPromo PromoFor(ChoiceData choice)
        {
            if (latestChoices == null || latestPromos == null || choice == null)
                return AppliedPromo.None;

            for (int i = 0; i < latestChoices.Count; i++)
            {
                if (latestChoices[i] == choice)
                    return PromoAt(i);
            }

            return AppliedPromo.None;
        }

        AppliedPromo PromoAt(int index)
        {
            if (latestPromos == null || index < 0 || index >= latestPromos.Count)
                return AppliedPromo.None;
            return latestPromos[index];
        }

        string FormatPay(float cost)
        {
            string cash = PocketFormat.Cash(cost);
            string template = payFormat ?? string.Empty;
            string formatted = template.IndexOf("{1}", StringComparison.Ordinal) >= 0
                ? string.Format(template, cash, string.Empty)
                : string.Format(template, cash);
            return formatted.Trim().TrimEnd('·', '\u00b7', ' ');
        }

        static string PlainLabel(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            int brace = text.IndexOf('{');
            if (brace < 0)
                return text;
            return text.Substring(0, brace).Trim().TrimEnd('·', '\u00b7', ' ');
        }
    }
}
