using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class IdleQueueManageCommand:AbstractCommand
    {
        public static readonly string StartParam = "1";
        public static readonly string StopParam = "0";

        private IdleManager _idler;
        private AccountModel _account;
        public IdleQueueManageCommand(AccountModel acc)
        {
            _account = acc;
            _idler = acc.Idler;

            _idler.PropertyChanged += IdlerManagerStateChanged;

            acc.IdleQueueBadges.CollectionChanged += (sender, args) => OnCanExecuteChanged(EventArgs.Empty);
        }

        private void IdlerManagerStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive")            
                OnCanExecuteChanged(EventArgs.Empty);            
        }

        public override bool CanExecute(object parameter)
        {
            var p = (string) parameter;
            if (p == StartParam)
                return !_idler.IsActive && _account.IdleQueueBadges.Count > 0;
            if (p == StopParam)
                return _idler.IsActive;
            return false;
        }

        public override void Execute(object parameter)
        {
            var p = (string)parameter;
            if (p == StartParam)
                _idler.Start();
            else if (p == StopParam)
                _idler.Stop();
        }
    }
}
