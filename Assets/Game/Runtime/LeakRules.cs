using System.Collections.Generic;

namespace SchoolDay
{
    public static class LeakRules
    {
        public static LeakTag NameLeak(IReadOnlyList<LeakTag> seen, IReadOnlyList<LeakTag> priority)
        {
            if (seen == null || priority == null)
                return LeakTag.None;

            for (int i = 0; i < priority.Count; i++)
            {
                LeakTag tag = priority[i];
                if (tag == LeakTag.None)
                    continue;

                for (int j = 0; j < seen.Count; j++)
                {
                    if (seen[j] == tag)
                        return tag;
                }
            }

            return LeakTag.None;
        }

        public static LeakTag DisplayOverspend(LeakTag tag)
        {
            if (tag == LeakTag.Grab || tag == LeakTag.Drinks)
                return tag;

            return LeakTag.None;
        }
    }
}
