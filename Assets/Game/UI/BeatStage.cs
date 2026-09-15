using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class BeatStage : MonoBehaviour
    {
        [SerializeField] DayConfig dayConfig;
        [SerializeField] Image background;
        [SerializeField] Text speaker;
        [SerializeField] Text title;
        [SerializeField] Text body;

        public void Bind(PresentedBeat presented)
        {
            if (presented == null || presented.Beat == null)
                return;

            gameObject.SetActive(true);

            BeatData beat = presented.Beat;
            Sprite backdrop = presented.Background != null ? presented.Background : beat.Background;
            if (presented.Choices != null && presented.Choices.Count == 1 && presented.Choices[0] != null && presented.Choices[0].Backdrop != null)
                backdrop = presented.Choices[0].Backdrop;

            if (background != null)
            {
                background.enabled = backdrop != null;
                background.sprite = backdrop;
            }

            if (speaker != null)
            {
                if (!string.IsNullOrEmpty(presented.SpeakerName))
                    speaker.text = presented.SpeakerName;
                else
                    speaker.text = dayConfig != null ? dayConfig.SpeakerName(beat.Speaker) : beat.Speaker.ToString();
            }

            if (title != null)
                title.text = !string.IsNullOrEmpty(presented.Title) ? presented.Title : beat.Title;
            if (body != null)
                body.text = !string.IsNullOrEmpty(presented.BodyLine) ? presented.BodyLine : beat.BodyLine;

            bool showClock = beat.Phase == BeatPhase.Title && !string.IsNullOrEmpty(presented.ClockDigits);
            DeskClockView clock = DeskClockView.On(background);
            if (clock != null)
            {
                if (showClock)
                    clock.Bind(presented.ClockDigits);
                else
                    clock.Hide();
            }
        }

        public void HideCopy()
        {
            if (speaker != null)
                speaker.text = string.Empty;
            if (title != null)
                title.text = string.Empty;
            if (body != null)
                body.text = string.Empty;
            DeskClockView clock = DeskClockView.On(background);
            if (clock != null)
                clock.Hide();
            gameObject.SetActive(false);
        }
    }
}
