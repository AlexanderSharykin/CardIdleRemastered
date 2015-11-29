using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CardIdleRemastered.Commands
{
    public class IdleStartCommand: AbstractCommand
    {
        private BadgeModel _badge;
        public IdleStartCommand(BadgeModel b)
        {
            _badge = b;
            _badge.CardIdleProcess.PropertyChanged += ProcessStateChanged;
        }

        private void ProcessStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsRunning")
            {
                OnCanExecuteChanged(EventArgs.Empty);
            }
        }

        public override bool CanExecute(object parameter)
        {
            return _badge.Account.IsSteamRunning && _badge.CardIdleProcess.IsRunning == false;
        }

        public override void Execute(object parameter)
        {
            _badge.CardIdleProcess.Start();
            _badge.Account.CheckActivityStatus();
        }        
    }
}
