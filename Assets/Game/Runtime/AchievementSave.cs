using System.Collections.Generic;
using UnityEngine;

namespace SchoolDay
{
    public static class AchievementSave
    {
        public const string PrefsKey = "SchoolDay.UnlockedAchievements";

        public static HashSet<string> Load()
        {
            var set = new HashSet<string>();
            string raw = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                return set;

            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i].Trim();
                if (!string.IsNullOrEmpty(id))
                    set.Add(id);
            }

            return set;
        }

        public static bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            HashSet<string> set = Load();
            if (!set.Add(id))
                return false;

            Persist(set);
            return true;
        }

        public static string CloseDay(AchievementCatalog catalog, DaySession session)
        {
            if (catalog == null || catalog.Items == null || session == null)
                return "";

            string shockId = "";
            string bufferId = "";
            for (int i = 0; i < catalog.Items.Length; i++)
            {
                AchievementData item = catalog.Items[i];
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                if (!Matches(item, session))
                    continue;

                Unlock(item.Id);
                if (item.UnlockOnBufferKid)
                    bufferId = item.Id;
                else
                    shockId = item.Id;
            }

            return !string.IsNullOrEmpty(shockId) ? shockId : bufferId;
        }

        static bool Matches(AchievementData item, DaySession session)
        {
            if (item.UnlockOnBufferKid)
                return session.IsBufferKid;

            if (!session.ShockSurvived || session.ShockChoice == null)
                return false;

            if (item.ShockChoice != null)
            {
                if (item.ShockChoice == session.ShockChoice)
                    return true;
                if (!string.IsNullOrEmpty(item.ShockChoice.Id) && item.ShockChoice.Id == session.ShockChoice.Id)
                    return true;
            }

            if (!string.IsNullOrEmpty(item.ChoiceId))
                return item.ChoiceId == session.ShockChoice.Id;

            return false;
        }

        static void Persist(HashSet<string> set)
        {
            var list = new List<string>(set);
            list.Sort();
            PlayerPrefs.SetString(PrefsKey, string.Join(",", list.ToArray()));
            PlayerPrefs.Save();
        }
    }
}
