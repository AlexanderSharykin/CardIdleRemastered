using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CardIdleRemastered
{
    public class AccountUpdater
    {
        private AccountModel _account;

        private DispatcherTimer _tmSync;
        private int _counter;
        private DispatcherTimer _tmCounter;
        private TimeSpan _interval;

        public AccountUpdater(AccountModel account)
        {
            _account = account;

            _tmSync = new DispatcherTimer();
            _tmSync.Tick += SyncBanges;
            Interval = new TimeSpan(0, 5, 0);
            //_tmSync.Start();

            _tmCounter = new DispatcherTimer();
            _tmCounter.Tick += UpdateSecondCounter;
            _tmCounter.Interval = new TimeSpan(0, 0, 1);
            //_tmCounter.Start();
        }

        public void Start()
        {            
            if (false == _tmSync.IsEnabled)
                _tmSync.Start();

            if (false == _tmCounter.IsEnabled)
            {
                _counter = 0;
                _tmCounter.Start();
            }            
        }

        public void Stop()
        {            
            _tmSync.Stop();            
            _tmCounter.Stop();
        }

        private void UpdateSecondCounter(object sender, EventArgs eventArgs)
        {            
            _counter++;
            int seconds = (int)Interval.TotalSeconds - _counter;
            if (seconds > 0)
            {
                var ts = TimeSpan.FromSeconds(seconds);
                _account.SyncTime = String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
            }
            else
                _account.SyncTime = "00:00";
        }

        public TimeSpan Interval
        {
            get { return _interval; }
            set
            {
                _interval = value;
                _tmSync.Interval = _interval;
            }
        }

        private async void SyncBanges(object sender, EventArgs eventArgs)
        {            
            _counter = 0;
            await Sync();            
        }

        public async Task Sync()
        {
            var tBadges = new SteamParser().LoadBadgesAsync(_account);
            var tProfile = new SteamParser().LoadProfileAsync(_account);
            await Task.WhenAll(tBadges, tProfile);
        }
    }
}
