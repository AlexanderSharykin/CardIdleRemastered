using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardIdleRemastered.Commands
{
    public class IdleProcess: ObservableModel
    {
        private BadgeModel _badge;
        private Process _steamIdle;

        public IdleProcess(BadgeModel badge)
        {
            _badge = badge;
        }

        public bool IsRunning
        {
            get { return _steamIdle != null && !_steamIdle.HasExited; }
        }

        public void Start()
        {
            if (IsRunning)
                return;

            _steamIdle = Process.Start(new ProcessStartInfo(@"steam-idle.exe", _badge.AppId.ToString()) { WindowStyle = ProcessWindowStyle.Hidden });  
            if (_steamIdle != null)
                _steamIdle.Exited += SteamIdleOnProcessExited;
            OnPropertyChanged("IsRunning");
        }

        private void SteamIdleOnProcessExited(object sender, EventArgs eventArgs)
        {            
            _steamIdle = null;
            OnPropertyChanged("IsRunning");
            OnIdleStopped(EventArgs.Empty);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _steamIdle.Exited -= SteamIdleOnProcessExited;
                _steamIdle.Kill();
                _steamIdle = null;                
                OnPropertyChanged("IsRunning");
                OnIdleStopped(EventArgs.Empty);
            }
        }

        public event EventHandler IdleStopped;

        protected virtual void OnIdleStopped(EventArgs e)
        {
            if (IdleStopped != null)
                IdleStopped(this, e);
        }
    }
}
