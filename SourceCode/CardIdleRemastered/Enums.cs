using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        Collected,
        Marketable,
        Owned
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
