using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class AchievementView : MonoBehaviour
    {
        [SerializeField] AchievementCatalog catalog;
        [SerializeField] Button openButton;
        [SerializeField] GameObject panel;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject dim;
        [SerializeField] AchievementRow rowPrefab;
        [SerializeField] Transform rowRoot;
        [SerializeField] string lockedLine = "Not yet";
        [SerializeField] Color lockedTint = new Color(0.42f, 0.43f, 0.46f, 1f);

        readonly List<AchievementRow> rows = new List<AchievementRow>();

        void Awake()
        {
            if (rowPrefab != null)
                rowPrefab.gameObject.SetActive(false);
            if (openButton != null)
                openButton.onClick.AddListener(ShowPanel);
            if (closeButton != null)
                closeButton.onClick.AddListener(HidePanel);
            HidePanel();
            SetTitleChrome(false);
        }

        public void SetTitleChrome(bool titleBeat)
        {
            if (openButton != null)
                openButton.gameObject.SetActive(titleBeat);
            if (!titleBeat)
                HidePanel();
        }

        public void Refresh()
        {
            if (catalog == null || catalog.Items == null)
                return;

            HashSet<string> unlocked = AchievementSave.Load();
            AchievementData[] items = Sorted(catalog.Items);
            for (int i = 0; i < items.Length; i++)
            {
                AchievementRow row = GetRow(i);
                AchievementData data = items[i];
                bool on = data != null && !string.IsNullOrEmpty(data.Id) && unlocked.Contains(data.Id);
                row.Bind(data, on, lockedLine, lockedTint);
            }

            for (int i = items.Length; i < rows.Count; i++)
                rows[i].gameObject.SetActive(false);
        }

        public void ShowPanel()
        {
            Refresh();
            if (dim != null)
                dim.SetActive(true);
            if (panel != null)
                panel.SetActive(true);
        }

        public void HidePanel()
        {
            if (dim != null)
                dim.SetActive(false);
            if (panel != null)
                panel.SetActive(false);
        }

        AchievementRow GetRow(int index)
        {
            while (rows.Count <= index)
            {
                AchievementRow created = Instantiate(rowPrefab, rowRoot != null ? rowRoot : transform);
                created.gameObject.SetActive(true);
                rows.Add(created);
            }

            return rows[index];
        }

        static AchievementData[] Sorted(AchievementData[] source)
        {
            var copy = new List<AchievementData>();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    copy.Add(source[i]);
            }

            copy.Sort(Compare);
            return copy.ToArray();
        }

        static int Compare(AchievementData a, AchievementData b)
        {
            int order = a.SortOrder.CompareTo(b.SortOrder);
            if (order != 0)
                return order;
            return string.CompareOrdinal(a.DisplayName, b.DisplayName);
        }
    }
}
