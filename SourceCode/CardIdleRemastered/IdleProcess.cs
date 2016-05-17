using System;
using System.Diagnostics;

namespace CardIdleRemastered
{
    public class IdleProcess: ObservableModel
    {
        private string  _appId;
        private Process _steamIdle;

        public IdleProcess(string appId)
        {
            _appId = appId;
        }

        public bool IsRunning
        {
            get { return _steamIdle != null && !_steamIdle.HasExited; }
        }

        public void Start()
        {
            if (IsRunning)
                return;

            _steamIdle = Process.Start(new ProcessStartInfo(@"steam-idle.exe", _appId) { WindowStyle = ProcessWindowStyle.Hidden });  
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
