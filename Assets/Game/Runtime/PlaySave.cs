using System;
using System.Globalization;
using UnityEngine;

namespace SchoolDay
{
    [Serializable]
    public sealed class PlaySaveData
    {
        public string screen;
        public string session_id;
        public string look_id;
        public string weekday;
        public string calendar_day;
        public int play_nonce;
        public int week_cursor;
        public bool cursor_set;
        public bool has_mix_carry;
        public float mix_pocket;
        public float mix_fuel;
        public float mix_face;
        public float mix_buffer;
        public int mix_leak;
        public bool mix_shock_reached;
        public bool mix_shock_survived;
        public float pocket;
        public float fuel;
        public float face;
        public float buffer;
        public float elapsed;
        public string[] choice_ids;
        public bool[] declined;
        public float[] choice_times;
    }

    public static class PlaySave
    {
        public const string PrefsKey = "SchoolDay.Play";
        public const string Select = "select";
        public const string Playing = "playing";
        public const string Ended = "ended";

        public static PlaySaveData Load()
        {
            string raw = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                return null;

            PlaySaveData data = JsonUtility.FromJson<PlaySaveData>(raw);
            if (data == null || string.IsNullOrEmpty(data.screen))
                return null;

            return data;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        public static void Write(PlaySaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.screen))
            {
                Clear();
                return;
            }

            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static DayCarry MixCarry(PlaySaveData data)
        {
            if (data == null || !data.has_mix_carry)
                return null;

            return new DayCarry
            {
                Stats = new MoneyStats
                {
                    Pocket = data.mix_pocket,
                    Fuel = data.mix_fuel,
                    Face = data.mix_face,
                    Buffer = data.mix_buffer
                },
                Leak = (LeakTag)data.mix_leak,
                ShockReached = data.mix_shock_reached,
                ShockSurvived = data.mix_shock_survived
            };
        }

        public static DateTime CalendarDay(PlaySaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.calendar_day))
                return DateTime.Now.Date;

            DateTime day;
            if (DateTime.TryParseExact(
                    data.calendar_day,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out day))
                return day.Date;

            return DateTime.Now.Date;
        }

        public static bool TryWeekday(PlaySaveData data, out SchoolWeekday weekday)
        {
            weekday = SchoolWeekday.Monday;
            if (data == null || string.IsNullOrEmpty(data.weekday))
                return false;

            return Enum.TryParse(data.weekday, out weekday);
        }

        public static string DateStamp(DateTime day)
        {
            return day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
