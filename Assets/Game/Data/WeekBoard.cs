using UnityEngine;

namespace SchoolDay
{
    [CreateAssetMenu(menuName = "School Day/Week Board", fileName = "WeekBoard")]
    public sealed class WeekBoard : ScriptableObject
    {
        public DayConfig Day;
        public int WeekSeed;
        public SchoolWeekday PreviewWeekday = SchoolWeekday.Monday;
        public bool UsePreviewWeekday;
        public WeekPlayMode PlayMode = WeekPlayMode.TodayOnly;
        public FridayWrap AfterFriday = FridayWrap.Stop;
        public SchoolWeekday WeekendFallback = SchoolWeekday.Monday;
        public int ReshuffleNonce;
        public string TitleHeadingFormat = "School Day";
        public string TitleBodyFormat = "{1} in pocket.";
        public BeatBinding Breakfast;
        public BeatBinding Transit;
        public BeatBinding Fomo;
        public BeatBinding Shock;
        public PromoCatalog Promos;

        public bool PlayFullWeek => PlayMode == WeekPlayMode.FullWeek;
    }
}
