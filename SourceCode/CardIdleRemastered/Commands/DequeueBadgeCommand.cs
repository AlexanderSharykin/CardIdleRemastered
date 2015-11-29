using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class DequeueBadgeCommand: AbstractCommand
    {
        private BadgeModel _badge;

        public DequeueBadgeCommand(BadgeModel badge)
        {
            _badge = badge;
        }

        public override bool CanExecute(object parameter)
        {
            return _badge.IsInQueue;
        }

        public override void Execute(object parameter)
        {
            _badge.Account.IdleQueueBadges.Remove(_badge);
            _badge.IsInQueue = false;
            OnCanExecuteChanged(EventArgs.Empty);
        }
    }
}
