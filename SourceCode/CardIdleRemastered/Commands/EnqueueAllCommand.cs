using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class EnqueueAllCommand:AbstractCommand
    {
        private AccountModel _account;

        public EnqueueAllCommand(AccountModel acc)
        {
            _account = acc;
        }

        public override void Execute(object parameter)
        {
            var order = (string) parameter;
            int idx = order == "0" ? 0 : _account.IdleQueueBadges.Count;
            foreach (BadgeModel badge in _account.Badges)
            {
                if (_account.IdleQueueBadges.Contains(badge) == false)
                {
                    _account.IdleQueueBadges.Insert(idx, badge);
                    badge.IsInQueue = true;
                    idx++;
                }
            }
        }
    }    
}
