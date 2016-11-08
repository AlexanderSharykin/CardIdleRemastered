using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered
{
    public enum BadgeProperty
    {
        Running,
        HasTrial,
        Enqueued,
        Blacklisted
    }

    public enum IdleMode
    {
        OneByOne,
        TrialFirst,
        TrialLast,
    }

    public enum FilterState
    {
        Any,
        Yes,
        No
    }
}
