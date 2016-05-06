using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered
{
    public enum BadgeModelFilter
    {
        All,
        Running,
        HasTrial,
        NotEnqueued,
        Blacklisted
    }

    public enum IdleMode
    {
        OneByOne,
        TrialFirst,
        TrialLast,
    }
}
