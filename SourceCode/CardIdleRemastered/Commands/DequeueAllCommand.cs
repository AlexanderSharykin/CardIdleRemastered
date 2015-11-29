using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class DequeueAllCommand: AbstractCommand
    {
        private AccountModel _account;
        public DequeueAllCommand(AccountModel acc)
        {
            _account = acc;
        }

        public override void Execute(object parameter)
        {
            foreach (BadgeModel badge in _account.Badges.OfType<BadgeModel>().Where(b => b.IsInQueue))
            {
                _account.IdleQueueBadges.Remove(badge);
                badge.IsInQueue = false;
            }            
        }
    }
}
