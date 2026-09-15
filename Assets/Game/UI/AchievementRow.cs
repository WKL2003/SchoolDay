using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class AchievementRow : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Text title;
        [SerializeField] Text line;

        public void Bind(AchievementData data, bool unlocked, string lockedLine, Color lockedTint)
        {
            gameObject.SetActive(true);
            if (data == null)
                return;

            if (icon != null)
            {
                icon.enabled = data.Icon != null;
                icon.sprite = data.Icon;
                icon.color = unlocked ? Color.white : lockedTint;
            }

            if (title != null)
            {
                title.text = data.DisplayName ?? "";
                title.color = unlocked ? new Color(0.93f, 0.9f, 0.84f) : new Color(0.62f, 0.6f, 0.56f);
            }

            if (line != null)
                line.text = unlocked ? (data.Description ?? "") : lockedLine;
        }
    }
}
