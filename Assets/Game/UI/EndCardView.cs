using System;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class EndCardView : MonoBehaviour
    {
        [SerializeField] ArtSet art;
        [SerializeField] Text lineCash;
        [SerializeField] Text lineShock;
        [SerializeField] Text lineLeak;
        [SerializeField] Text lineReputation;
        [SerializeField] Text coachSpeaker;
        [SerializeField] Text coachLine;
        [SerializeField] Image badge;
        [SerializeField] Text badgeLabel;
        [SerializeField] Text resultTitle;
        [SerializeField] Button nextMorningButton;
        [SerializeField] string winTitle = "You still have $2.";
        [SerializeField] string loseTitle = "School got expensive.";
        [SerializeField] string cashFormat = "Cash left: {0}";
        [SerializeField] string shockPaidFormat = "Surprise bill: paid {0}.";
        [SerializeField] string shockMissFormat = "Surprise bill: skipped {0}.";
        [SerializeField] string leakFormat = "Overspend: {0}.";
        [SerializeField] string reputationFormat = "Reputation: {0}%.";
        [SerializeField] string badgeText = "Kept $2";
        [SerializeField] string unknownShock = "the surprise bill";

        Action nextMorning;

        void Awake()
        {
            if (nextMorningButton != null)
                nextMorningButton.onClick.AddListener(HandleNextMorning);
        }

        public void Wire(Action onNextMorning)
        {
            nextMorning = onNextMorning;
        }

        public void Show(DaySession session)
        {
            gameObject.SetActive(true);
            DayConfig config = session.Config;

            if (resultTitle != null)
                resultTitle.text = session.Won ? winTitle : loseTitle;

            if (lineCash != null)
                lineCash.text = string.Format(cashFormat, PocketFormat.Cash(session.Stats.Pocket));

            if (lineShock != null)
            {
                string shockName = session.ShockChoice != null ? session.ShockChoice.DisplayName : unknownShock;
                lineShock.text = string.Format(session.ShockSurvived ? shockPaidFormat : shockMissFormat, shockName);
            }

            if (lineLeak != null)
            {
                LeakTag shown = LeakRules.DisplayOverspend(session.NamedLeak);
                string name = config.LeakName(shown);
                if (shown == LeakTag.None || string.IsNullOrEmpty(name) || name == "none")
                    name = "None";
                lineLeak.text = string.Format(leakFormat, name);
            }

            if (lineReputation != null)
                lineReputation.text = string.Format(reputationFormat, session.Reputation.Percent);

            CoachLine coach = session.Coach;
            if (coachSpeaker != null)
                coachSpeaker.text = string.IsNullOrEmpty(coach.Line) ? string.Empty : config.SpeakerName(coach.Speaker);
            if (coachLine != null)
                coachLine.text = coach.Line ?? string.Empty;

            bool showBadge = session.IsBufferKid;
            if (badge != null)
            {
                badge.gameObject.SetActive(showBadge);
                if (showBadge && art != null && art.BadgeBufferKid != null)
                    badge.sprite = art.BadgeBufferKid;
            }

            if (badgeLabel != null)
            {
                badgeLabel.gameObject.SetActive(showBadge);
                badgeLabel.text = badgeText;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void HandleNextMorning()
        {
            nextMorning?.Invoke();
        }
    }
}
