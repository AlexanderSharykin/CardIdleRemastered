using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CardIdleRemastered.Properties;
using Steamworks;
using CardIdleRemastered.Commands;

namespace CardIdleRemastered
{
    public class AccountModel:ObservableModel
    {        
        private AccountUpdater _updater;
        private IdleManager _idler;
        private string _userName;
        private string _level = "0";
        private BitmapImage _avatar;

        private ICollectionView _badges;
        private BadgeModelFilter _filter;
        
        private bool _isCardIdleActive;

        private DispatcherTimer _tmSteamStatus;
        private ICommand _logoutCmd;
        private ICommand _enqueueCmd;
        private ICommand _dequeueCmd;
        private ICommand _idleCmd;        
        private int _totalCards;
        private int _totalGames;
        private BitmapImage _background;
        private string _syncTime;
        private string _gameTitle;
        private BitmapImage _customBackground;
        private string _avatarUrl;
        private string _backgroundUrl;
        private string _customBackgroundUrl;

        public AccountModel()
        {
            _updater = new AccountUpdater(this);
            _idler = new IdleManager(this);

            AllBadges = new ObservableCollection<BadgeModel>();

            IdleQueueBadges = new ObservableCollection<BadgeModel>();
            _filter = BadgeModelFilter.All;

            
            _badges = CollectionViewSource.GetDefaultView(AllBadges);
            var quick = (ICollectionViewLiveShaping)_badges;
            quick.LiveFilteringProperties.Add("IsBlacklisted");
            quick.LiveFilteringProperties.Add("HasTrial");
            quick.LiveFilteringProperties.Add("CardIdleActive");
            quick.LiveFilteringProperties.Add("IsInQueue");
            quick.IsLiveFiltering = true;
        }

        private bool TitleSearch(object o)
        {
            return string.IsNullOrWhiteSpace(GameTitle) || ((BadgeModel) o).Title.ToLower().Contains(GameTitle);
        }

        private bool IsTrialGame(object o)
        {
            return ((BadgeModel) o).HasTrial && TitleSearch(o);
        }

        private bool IsRunningGame(object o)
        {
            return ((BadgeModel)o).CardIdleActive && TitleSearch(o);
        }

        private bool NotEnqueuedGame(object o)
        {
            return ((BadgeModel)o).IsInQueue == false && TitleSearch(o);
        }

        private bool IsBlacklistedGame(object o)
        {
            return ((BadgeModel)o).IsBlacklisted && TitleSearch(o);
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        public string ProfileUrl { get; set; }

        public BitmapImage Avatar
        {
            get { return _avatar; }
            set
            {
                _avatar = value;
                OnPropertyChanged();
            }
        }

        public string AvatarUrl
        {
            get { return _avatarUrl; }
            set
            {
                _avatarUrl = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage Background
        {
            get
            {
                if (_customBackground != null)
                    return _customBackground;
                return _background;
            }
            set
            {
                _background = value;                
                OnPropertyChanged();
            }
        }

        public string BackgroundUrl
        {
            get { return _backgroundUrl; }
            set
            {
                _backgroundUrl = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage CustomBackground
        {
            get { return _customBackground; }
            set
            {
                _customBackground = value;
                OnPropertyChanged("Background");
            }
        }

        public string CustomBackgroundUrl
        {
            get { return _customBackgroundUrl; }
            set
            {
                _customBackgroundUrl = value;
                OnPropertyChanged();
            }
        }

        public string Level
        {
            get { return _level; }
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView Badges { get { return _badges; } }

        public ObservableCollection<BadgeModel> AllBadges { get; private set; }
        
        public ObservableCollection<BadgeModel> IdleQueueBadges { get; private set; }

        public int TotalCards
        {
            get { return _totalCards; }
            set
            {
                _totalCards = value; 
                OnPropertyChanged();
            }
        }

        public int TotalGames
        {
            get { return _totalGames; }
            set
            {
                _totalGames = value; 
                OnPropertyChanged();
            }
        }

        public void UpdateTotalValues()
        {
            TotalGames = Badges.Cast<BadgeModel>().Count();
            TotalCards = Badges.Cast<BadgeModel>().Sum(b => b.RemainingCard);
        }

        public string SyncTime
        {
            get { return _syncTime; }
            set
            {
                _syncTime = value; 
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> BadgeFilters
        {
            get
            {
                return Enum.GetValues(typeof (BadgeModelFilter))
                    .Cast<BadgeModelFilter>()
                    .Select(e => EnumLocalizationConverter.GetLocalValue(e));
            }
        }

        public BadgeModelFilter Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                OnPropertyChanged();
                SetFilter();                
            }
        }

        private void SetFilter()
        {
            switch (_filter)
            {
                case BadgeModelFilter.All:
                    _badges.Filter = TitleSearch;
                    break;
                case BadgeModelFilter.HasTrial:
                    _badges.Filter = IsTrialGame;
                    break;
                case BadgeModelFilter.Running:
                    _badges.Filter = IsRunningGame;
                    break;
                case BadgeModelFilter.NotEnqueued:
                    _badges.Filter = NotEnqueuedGame;
                    break;
                case BadgeModelFilter.Blacklisted:
                    _badges.Filter = IsBlacklistedGame;
                    break;
            }            
            UpdateTotalValues();
        }

        public string GameTitle
        {
            get { return _gameTitle; }
            set
            {
                value = value.Trim().ToLower();
                if (_gameTitle == value)
                    return;
                _gameTitle = value;
                SetFilter();
                OnPropertyChanged();
            }
        }

        public bool IsCardIdleActive
        {
            get
            {
                return AllBadges.Any(b=>b.CardIdleActive);
            }
        }

        public void CheckActivityStatus()
        {
            OnPropertyChanged("IsCardIdleActive");
        }

        public bool IsSteamRunning
        {
            get
            {
                return SteamAPI.IsSteamRunning() || IgnoreClient;
            }
        }

        public bool IgnoreClient { get; set; }

        public IdleManager Idler
        {
            get { return _idler; }
        }

        public void InitSteamTimer()
        {
            _tmSteamStatus = new DispatcherTimer();
            _tmSteamStatus.Interval = new TimeSpan(0,0,5);
            bool steamRunning = false;
            _tmSteamStatus.Tick += (sender, args) =>
            {
                bool connected = IsSteamRunning;
                if (steamRunning != connected)
                    OnPropertyChanged("IsSteamRunning");
                steamRunning = connected;
            };
            _tmSteamStatus.Start();
        }

        public async Task Sync()
        {
            await _updater.Sync();
        }

        public Action<BadgeModel> BlacklistBadge { get; set; }

        public ICommand LogoutCmd
        {
            get
            {
                if (_logoutCmd == null)
                    _logoutCmd = new LogoutCommand(this);
                return _logoutCmd;
            }            
        }

        public ICommand EnqueueCmd
        {
            get
            {
                if (_enqueueCmd == null)
                    _enqueueCmd = new EnqueueAllCommand(this);
                return _enqueueCmd;
            }
        }

        public ICommand DequeueCmd
        {
            get
            {
                if (_dequeueCmd == null)
                    _dequeueCmd = new DequeueAllCommand(this);
                return _dequeueCmd;
            }
        }


        public ICommand IdleCmd
        {
            get
            {
                if (_idleCmd == null)
                    _idleCmd = new IdleQueueManageCommand(this);
                return _idleCmd;
            }
        }

        public void StartTimers()
        {
            _updater.Start();
        }

        public void StopTimers()
        {
            _updater.Stop();
        }

        public void StopCardIdle()
        {
            Idler.Stop();
            foreach (var badge in AllBadges)
            {
                if (badge.CardIdleActive)
                    badge.CardIdleProcess.Stop();
            }
        }
    }
}
