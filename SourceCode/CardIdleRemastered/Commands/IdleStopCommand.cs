using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CardIdleRemastered.Commands
{
    public class IdleStopCommand: AbstractCommand
    {
        private BadgeModel _badge;
        public IdleStopCommand(BadgeModel b)
        {
            _badge = b;
            _badge.CardIdleProcess.PropertyChanged += ProcessStateChanged;
        }

        private void ProcessStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsRunning")
            {
                OnCanExecuteChanged(EventArgs.Empty);
                _badge.Account.CheckActivityStatus();
            }
        }

        public override bool CanExecute(object parameter)
        {
            return _badge.CardIdleProcess.IsRunning;
        }

        public override void Execute(object parameter)
        {
            _badge.CardIdleProcess.Stop();            
        }

    }
}
