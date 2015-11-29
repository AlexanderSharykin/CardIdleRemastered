using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CardIdleRemastered.Commands
{
    public abstract class AbstractCommand: ICommand
    {
        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public abstract void Execute(object parameter);
        
        public event EventHandler CanExecuteChanged;

        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, e);
        }
    }
}
