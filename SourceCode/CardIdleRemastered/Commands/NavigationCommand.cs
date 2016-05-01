using System;
using System.Diagnostics;

namespace CardIdleRemastered.Commands
{
    public class NavigationCommand: BaseCommand
    {
        public override void Execute(object parameter)
        {
            var path = parameter as string;
            if (String.IsNullOrWhiteSpace(path) == false)
                Process.Start(new ProcessStartInfo(path));
        }
    }
}
