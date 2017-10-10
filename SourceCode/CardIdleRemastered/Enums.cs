using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered
{
    public static class Enums
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).OfType<T>();
        }
    }

    public enum BadgeProperty
    {
        Running,
        HasTrial,
        Enqueued,
        Blacklisted
    }

    public enum ShowcaseProperty
    {
        Completed,
        Bookmarked,
        Collected
    }

    public enum IdleMode
    {
        OneByOne,
        TrialFirst,
        TrialLast,
        PeriodicSwitch,
        All
    }

    public enum FilterState
    {
        Any,
        Yes,
        No
    }
}
