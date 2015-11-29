using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class EnqueueBadgeCommand:AbstractCommand
    {
        private BadgeModel _badge;        
        public EnqueueBadgeCommand(BadgeModel badge)
        {
            _badge = badge;
            _badge.PropertyChanged += BadgeQueueStatusChanged;
        }

        private void BadgeQueueStatusChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsInQueue")
                OnCanExecuteChanged(EventArgs.Empty);
        }

        public override bool CanExecute(object parameter)
        {
            return !_badge.IsInQueue;
        }

        public override void Execute(object parameter)
        {
            var order = (string) parameter;
            var acc = _badge.Account;
            if (acc.IdleQueueBadges.Contains(_badge) == false)
            {
                if (order == "0")
                    acc.IdleQueueBadges.Insert(0, _badge);
                else 
                    acc.IdleQueueBadges.Add(_badge);
                _badge.IsInQueue = true;
            }
            OnCanExecuteChanged(EventArgs.Empty);
        }
    }
}
