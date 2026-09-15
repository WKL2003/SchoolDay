using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SchoolDay
{
    public sealed class ExpenseLogView : MonoBehaviour
    {
        [SerializeField] Text rowPrefab;
        [SerializeField] Transform rowRoot;
        [SerializeField] ArtSet art;
        [SerializeField] string rowFormat = "{0}  {1}  {2}";

        readonly List<Text> rows = new List<Text>();

        void Awake()
        {
            if (rowPrefab != null)
                rowPrefab.gameObject.SetActive(false);
        }

        public void Clear()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].gameObject.SetActive(false);
        }

        public void Append(ExpenseEntry entry)
        {
            if (entry == null)
                return;

            Text row = GetRow();
            string stamp = entry.Time.ToString("0s", CultureInfo.InvariantCulture);
            string amount = entry.Amount <= 0.001f
                ? PocketFormat.Cash(0f)
                : PocketFormat.PriceDelta(entry.Amount);
            row.text = string.Format(rowFormat, stamp, entry.Label, amount);
            if (art != null)
                row.color = entry.Amount > 0.001f ? art.PriceOk : art.Muted;
            StartCoroutine(Pop(row.transform));
        }

        static IEnumerator Pop(Transform row)
        {
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                float u = t / 0.18f;
                float scale = 1f + 0.12f * Mathf.Sin(u * Mathf.PI);
                row.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            row.localScale = Vector3.one;
        }

        Text GetRow()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (!rows[i].gameObject.activeSelf)
                {
                    rows[i].gameObject.SetActive(true);
                    return rows[i];
                }
            }

            Text created = Instantiate(rowPrefab, rowRoot != null ? rowRoot : transform);
            created.gameObject.SetActive(true);
            rows.Add(created);
            return created;
        }
    }
}
