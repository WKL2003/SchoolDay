using System.Collections.Generic;

namespace SchoolDay
{
    public readonly struct PromoBias
    {
        public static readonly PromoBias None = default;

        public PromoBias(HashSet<string> groups, int weight)
        {
            Groups = groups;
            Weight = weight < 1 ? 1 : weight;
        }

        public HashSet<string> Groups { get; }
        public int Weight { get; }
    }
}
