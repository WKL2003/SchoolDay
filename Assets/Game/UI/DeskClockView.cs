using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class DeskClockView : MonoBehaviour
    {
        static readonly Vector2 AnchorMin = new Vector2(0.748f, 0.414f);
        static readonly Vector2 AnchorMax = new Vector2(0.800f, 0.452f);
        static readonly Color Plate = new Color(0.06f, 0.07f, 0.08f, 1f);
        static readonly Color Digits = new Color(0.78f, 0.84f, 0.88f, 1f);

        Text digits;

        public static DeskClockView On(Graphic host)
        {
            if (host == null)
                return null;

            DeskClockView view = host.GetComponentInChildren<DeskClockView>(true);
            if (view != null)
                return view;

            var plate = new GameObject("DeskClock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DeskClockView));
            plate.layer = host.gameObject.layer;
            plate.transform.SetParent(host.transform, false);

            view = plate.GetComponent<DeskClockView>();
            view.Build(host);
            return view;
        }

        public void Bind(string clockDigits)
        {
            if (digits != null)
                digits.text = clockDigits ?? "";
            gameObject.SetActive(!string.IsNullOrEmpty(clockDigits));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Build(Graphic host)
        {
            var plate = GetComponent<RectTransform>();
            plate.anchorMin = AnchorMin;
            plate.anchorMax = AnchorMax;
            plate.offsetMin = Vector2.zero;
            plate.offsetMax = Vector2.zero;

            var fill = GetComponent<Image>();
            fill.color = Plate;
            fill.raycastTarget = false;

            var label = new GameObject("Digits", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.layer = gameObject.layer;
            label.transform.SetParent(transform, false);

            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            digits = label.GetComponent<Text>();
            digits.font = UiFont();
            digits.fontSize = 18;
            digits.fontStyle = FontStyle.Bold;
            digits.alignment = TextAnchor.MiddleCenter;
            digits.color = Digits;
            digits.raycastTarget = false;
            digits.horizontalOverflow = HorizontalWrapMode.Overflow;
            digits.verticalOverflow = VerticalWrapMode.Overflow;
            digits.resizeTextForBestFit = true;
            digits.resizeTextMinSize = 8;
            digits.resizeTextMaxSize = 28;

            if (host != null)
                transform.SetAsLastSibling();
        }

        static Font UiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
