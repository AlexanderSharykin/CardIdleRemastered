using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class SetPriorityCommand: AbstractCommand
    {
        private BadgeModel _badge;
        private static readonly string _upParam = "-1";
        private static readonly string _downParam = "1";

        public SetPriorityCommand(BadgeModel badge)
        {
            _badge = badge;
        }

        public override bool CanExecute(object parameter)
        {
            var list = _badge.Account.IdleQueueBadges;
            if (list.Count < 2)
                return false;

            string order = (string) parameter;

            if (order == _upParam)
                return list[0] != _badge;

            if (order == _downParam)
                return list[list.Count - 1] != _badge;

            return false;
        }

        public override void Execute(object parameter)
        {
            var list = _badge.Account.IdleQueueBadges;
            int idx = list.IndexOf(_badge);
            string order = (string)parameter;

            list.RemoveAt(idx);
            if (order == _upParam)
                list.Insert(idx - 1, _badge);
            else
                list.Insert(idx + 1, _badge);
            OnCanExecuteChanged(EventArgs.Empty);
        }
    }
}
