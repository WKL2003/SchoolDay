using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] DayConfig dayConfig;
        [SerializeField] ArtSet art;
        [SerializeField] Text pocketValue;
        [SerializeField] Text fuelValue;
        [SerializeField] Text faceValue;
        [SerializeField] Text bufferValue;
        [SerializeField] Image fuelFill;
        [SerializeField] Image faceFill;
        [SerializeField] Image bufferFill;
        [SerializeField] Image pocketIcon;
        [SerializeField] Image fuelIcon;
        [SerializeField] Image faceIcon;
        [SerializeField] Image bufferIcon;
        [SerializeField] Text tooltip;
        [SerializeField] float tweenSeconds = 0.28f;
        [SerializeField] float tipHideSeconds = 5f;
        [SerializeField] string bufferFormat = "{0} / {1}";

        MoneyStats shown;
        float shownReputation;
        Coroutine tween;
        Coroutine tipHide;
        Color pocketIdle;

        void Awake()
        {
            if (pocketValue != null)
                pocketIdle = pocketValue.color;
            WireTip(pocketIcon, dayConfig != null ? dayConfig.BudgetTip : null);
            WireTip(fuelIcon, dayConfig != null ? dayConfig.EnergyTip : null);
            WireTip(faceIcon, dayConfig != null ? dayConfig.ReputationTip : null);
            WireTip(bufferIcon, dayConfig != null ? dayConfig.KeepTip : null);
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        void WireTip(Image icon, string tip)
        {
            if (icon == null)
                return;
            Button button = icon.GetComponent<Button>();
            if (button == null)
                return;
            string captured = tip;
            button.onClick.AddListener(() => ToggleTip(captured));
        }

        void OnDisable()
        {
            StopTipHide();
        }

        void ToggleTip(string tip)
        {
            if (tooltip == null)
                return;
            bool same = tooltip.gameObject.activeSelf && tooltip.text == tip;
            bool show = !same && !string.IsNullOrEmpty(tip);
            tooltip.gameObject.SetActive(show);
            if (!show)
            {
                StopTipHide();
                return;
            }

            tooltip.text = tip ?? "";
            RestartTipHide();
        }

        void RestartTipHide()
        {
            StopTipHide();
            if (!isActiveAndEnabled)
                return;
            tipHide = StartCoroutine(HideTipAfterDelay());
        }

        void StopTipHide()
        {
            if (tipHide == null)
                return;
            StopCoroutine(tipHide);
            tipHide = null;
        }

        IEnumerator HideTipAfterDelay()
        {
            yield return new WaitForSecondsRealtime(tipHideSeconds);
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
            tipHide = null;
        }

        public void UseLook(CharacterLook look)
        {
            if (faceIcon == null || look == null)
                return;

            Sprite face = look.HudFace != null ? look.HudFace : look.Idle;
            if (face != null)
                faceIcon.sprite = face;
        }

        public void Bind(MoneyStats stats, bool animate)
        {
            Bind(stats, default, animate);
        }

        public void Bind(MoneyStats stats, ReputationScore reputation, bool animate)
        {
            if (stats == null)
                return;

            float nextReputation = reputation.Percent;
            if (!animate || shown == null)
            {
                shown = stats.Clone();
                shownReputation = nextReputation;
                Paint(shown, shownReputation);
                return;
            }

            MoneyStats from = shown.Clone();
            float fromReputation = shownReputation;
            shown = stats.Clone();
            shownReputation = nextReputation;
            if (tween != null)
                StopCoroutine(tween);
            tween = StartCoroutine(Tween(from, shown, fromReputation, shownReputation));
        }

        IEnumerator Tween(MoneyStats from, MoneyStats to, float reputationFrom, float reputationTo)
        {
            float t = 0f;
            var step = new MoneyStats();
            while (t < tweenSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / tweenSeconds);
                step.Pocket = Mathf.Lerp(from.Pocket, to.Pocket, u);
                step.Fuel = Mathf.Lerp(from.Fuel, to.Fuel, u);
                step.Face = Mathf.Lerp(from.Face, to.Face, u);
                step.Buffer = Mathf.Lerp(from.Buffer, to.Buffer, u);
                Paint(step, Mathf.Lerp(reputationFrom, reputationTo, u));
                yield return null;
            }

            Paint(to, reputationTo);
            tween = null;
        }

        void Paint(MoneyStats stats, float reputation)
        {
            float cap = dayConfig != null ? Mathf.Max(0.01f, dayConfig.StatCap) : 1f;
            float pocketCap = dayConfig != null ? Mathf.Max(0.01f, dayConfig.StartingPocket) : 1f;

            if (pocketValue != null)
            {
                pocketValue.text = PocketFormat.Cash(stats.Pocket);
                Color idle = pocketIdle.a > 0.01f ? pocketIdle : (art != null ? art.PriceOk : Color.white);
                pocketValue.color = stats.Pocket < -0.001f && art != null
                    ? art.PriceBad
                    : idle;
            }
            if (fuelValue != null)
                fuelValue.text = Mathf.RoundToInt(stats.Fuel).ToString();
            if (faceValue != null)
                faceValue.text = Mathf.RoundToInt(reputation).ToString();
            if (bufferValue != null && dayConfig != null)
                bufferValue.text = string.Format(bufferFormat, PocketFormat.Cash(stats.Buffer), PocketFormat.Cash(dayConfig.BufferWin));

            SetFill(fuelFill, stats.Fuel / cap);
            SetFill(faceFill, reputation / cap);
            SetFill(bufferFill, stats.Buffer / pocketCap);
        }

        static void SetFill(Image fill, float amount)
        {
            if (fill == null)
                return;
            fill.fillAmount = Mathf.Clamp01(amount);
        }
    }
}
