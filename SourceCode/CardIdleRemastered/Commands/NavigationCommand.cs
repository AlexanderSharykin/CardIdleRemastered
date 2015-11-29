using System.Diagnostics;

namespace CardIdleRemastered.Commands
{
    public class NavigationCommand: AbstractCommand
    {
        public string DefaultUri { get; set; }

        public override void Execute(object parameter)
        {
            Process.Start(new ProcessStartInfo((string)parameter ?? DefaultUri));
        }
    }
}
