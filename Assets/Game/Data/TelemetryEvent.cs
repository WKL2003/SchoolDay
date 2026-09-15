using System;

namespace SchoolDay
{
    [Serializable]
    public sealed class TelemetryEvent
    {
        public string session_id;
        public string player_id;
        public string kind;
        public string ts;
        public string weekday;
        public string look_id;
        public string beat_id;
        public string beat_name;
        public string choice_id;
        public string choice_name;
        public float cost;
        public float pocket;
        public float fuel;
        public float face;
        public float buffer;
        public bool went_broke;
        public bool declined;
        public bool shock_survived;
        public string named_leak;
        public bool day_ended;
        public bool won;
        public bool buffer_kid;
        public float duration;
        public string achievement_id;
        public string promo_id;
        public float reputation;
        public float punctual;
        public float friendly;
        public float adaptive;
        public float generous;
    }
}
