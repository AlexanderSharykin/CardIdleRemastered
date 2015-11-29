using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered.Commands
{
    public class BlackListCommand:AbstractCommand
    {
        private BadgeModel _badge;
        public BlackListCommand(BadgeModel badge)
        {
            _badge = badge;
        }

        public override void Execute(object parameter)
        {
            _badge.IsBlacklisted = !_badge.IsBlacklisted;

            _badge.Account.BlacklistBadge(_badge);
        }
    }
}
