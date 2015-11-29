using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CardIdleRemastered.Properties;

namespace CardIdleRemastered.Commands
{
    public class LogoutCommand: ICommand
    {
        private AccountModel _account;

        public LogoutCommand(AccountModel acc)
        {
            _account = acc;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _account.StopCardIdle();
            _account.StopTimers();            

            App.CardIdle.Logout();
        }

        public event EventHandler CanExecuteChanged;
    }
}
