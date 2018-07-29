using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CardIdleRemastered
{
    public class AccountUpdater
    {
        private AccountModel _account;
        private PricesUpdater _pricesUpdater;
        private DispatcherTimer _tmSync;
        private DispatcherTimer _tmCounter;
        private TimeSpan _interval;
        private int _counter;

        public AccountUpdater(AccountModel account, PricesUpdater pricesUpdater)
        {
            _account = account;
            _pricesUpdater = pricesUpdater;

            _tmSync = new DispatcherTimer();
            _tmSync.Tick += SyncBanges;
            Interval = new TimeSpan(0, 5, 0);

            _tmCounter = new DispatcherTimer();
            _tmCounter.Tick += UpdateSecondCounter;
            _tmCounter.Interval = new TimeSpan(0, 0, 1);
        }

        public IList<BadgeModel> CompleBadgeList { get; private set; }

        public event Action BadgeListSync;

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
            await Sync(true);
        }

        private bool _syncRunning;
        public async Task Sync(bool resetCounter)
        {
            if (_syncRunning)
                return;

            if (resetCounter)
                _counter = 0;

            Task<IEnumerable<BadgeModel>> tBadges = null;
            try
            {
                _syncRunning = true;

                tBadges = LoadBadgesAsync();
                var tProfile = LoadProfileAsync();
                await Task.WhenAll(tBadges, tProfile);

                CompleBadgeList = tBadges.Result.ToList();
                if (BadgeListSync != null)
                    BadgeListSync();
            }
            finally
            {
                _syncRunning = false;
            }
        }

        private async Task LoadProfileAsync()
        {
            var profile = await new SteamParser().LoadProfileAsync(_account.ProfileUrl);
            _account.BackgroundUrl = profile.BackgroundUrl;
            _account.AvatarUrl = profile.AvatarUrl;
            _account.UserName = profile.UserName;
            _account.Level = profile.Level;

            if (profile.BadgeUrl != null)
            {
                _account.FavoriteBadge = new BadgeLevelData
                                         {
                                             Name = profile.BadgeTitle,
                                             PictureUrl = profile.BadgeUrl
                                         };
            }
        }

        private async Task<IEnumerable<BadgeModel>> LoadBadgesAsync()
        {
            var badges = await new SteamParser().LoadBadgesAsync(_account.ProfileUrl);

            foreach (var badge in badges)
            {
                var b = _account.AllBadges.FirstOrDefault(x => x.AppId == badge.AppId);
                if (badge.RemainingCard > 0)
                {
                    if (b == null)
                    {
                        _account.AddBadge(badge);
                    }
                    else
                    {
                        b.RemainingCard = badge.RemainingCard;
                        b.HoursPlayed = badge.HoursPlayed;
                        b.CardsCurrent = badge.CardsCurrent;
                    }
                }
                else
                {
                    if (b != null)
                        _account.RemoveBadge(b);
                }
                var stock = _pricesUpdater.GetStockModel(badge.AppId);
                if (stock != null)
                {
                    badge.CardPrice = Math.Round(stock.Normal / stock.Count, 2);
                    badge.BadgePrice = stock.Normal;
                }
            }

            _account.UpdateTotalValues();
            _account.UpdateTrialStatus();

            return badges;
        }
    }
}
